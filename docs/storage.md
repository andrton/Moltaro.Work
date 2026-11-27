# Storage

**Schema version 1.** What the provider contract does to the items of a queue, what the tables of
the schema mean, and what each database needs. The schema is part of the public contract: an
application creates the tables with a provider's script and may read them.

**What exists.** The provider contract `IWorkStore` of the core, and three providers with their
schema scripts: `Moltaro.Work.PostgreSql`, `Moltaro.Work.SqlServer` and `Moltaro.Work.MySql`. No
operation is implemented yet: every operation of their stores throws `NotSupportedException`, and
each is planned.

## The provider contract

`IWorkStore` is implemented once per database by a provider package. Every method may be called
concurrently.

- **Transactions.** An enqueue runs on the caller's connection, inside the caller's transaction, at
  its isolation. Every other operation runs on a connection and in a transaction of the store's
  own, at the isolation the provider names.
- **Time.** Every time is the database's. Due times, lease ends and retention are compared with the
  database's clock; no operation takes an instant from the caller but the due time of an enqueue,
  where `null`, or an instant the database's clock has passed, means its now.
- **The fence.** A fenced write — heartbeat, complete, retry, give up, release — applies only to an
  item that is held with the claim's fence: held, and its fence is the one the claim was given.
  Otherwise it writes nothing and reports the refusal: `false`, or the claim among those
  `HeartbeatAsync` returns.
- **Errors.** A failure of the database reaches the caller as the driver's `DbException`, through
  the task. An enqueue neither catches nor retries it: the caller rolls its transaction back and
  retries it, as for any write. A refusal of a requeue is a status, never a unique violation.

What each operation does to an item, all times by the database's clock:

| Operation | Applies to | Effect |
| --- | --- | --- |
| Enqueue | No row with that active key | A waiting row; `due_at` the later of the due time and now |
| | Coalescing, waiting | `due_at` the earlier of the two, unless it waits for a retry (attempts above zero) |
| | Coalescing, held | `again_due_at` the earlier of the two |
| | Coalescing, dead | Waiting, `due_at` as a new one, `attempts` 0, the reason kept |
| | Log or run, active key | Nothing; `Coalesced` |
| Claim | Waiting with `due_at` reached, or held with `lease_until` reached | Held; `fence` and `attempts` plus one; `lease_until` now plus the lease; `claimed_at` now; at most one item per scope, with its scope row |
| Heartbeat | Held, with the claim's fence | `lease_until` now plus the lease |
| Complete | Held, with the claim's fence | Coalescing: deleted, or waiting at `again_due_at` with `attempts` 0; log: deleted; run: finished |
| Retry | Held, with the claim's fence | Waiting at now plus the delay, with the reason |
| Give up | Held, with the claim's fence | Dead with the reason; a coalescing item with a mark is revived at once, as by an enqueue: waiting at `again_due_at`, `attempts` 0, the reason kept |
| Release | Held, with the claim's fence | Waiting at now, with the attempts it had before the claim; a mark stays, as with a retry |
| Requeue | Dead | Waiting at now, `attempts` 0, the reason kept |
| Delete expired | Finished or dead, `finished_at` older than the retention | Deleted, at most the batch, oldest first |

A fenced write that ends a hold also deletes the item's scope row. A finished or dead log or run
item gives up its `active_key`. An enqueue reports `Added` when an item became waiting that was
neither waiting nor held, and `Coalesced` with the id of the item that absorbed it. A requeue
reports `NotDead` when the queue has no dead item with that id, and `KeyActive` when a dead log or
run item's key belongs to an active item.

Two operations change no item. `CheckDatabaseAsync` checks the schema version and the settings the
provider needs, and names in its `InvalidOperationException` what it found. `GetNextDueAsync`
returns two durations from the database's now: until an item may be claimed, zero once a lease has
ended and never the time until a lease ends, and until an item is expired.

## The schema, version 1

The same three tables in every database, created by the provider's script in the connection's
default schema. The section of each database below has its script.

`moltaro_work_item` holds the items of every queue:

| Column | Meaning |
| --- | --- |
| `id` | The item's id, assigned by the database. With `fence` it names one claim, which never repeats |
| `queue` | The name of the item's queue |
| `state` | 1 waiting, 2 held, 3 finished, 4 dead. A coalescing item is never finished: it is deleted |
| `item_key` | The item's key; `NULL` for a log or run item without one |
| `active_key` | Reserves the key. For a coalescing item it is `item_key` in every state, one row per key; for a log or run item it is `item_key` while waiting or held and `NULL` otherwise. Its unique index is the one mechanism of "one per key" in every provider |
| `scope` | The item's scope, or `NULL` |
| `payload` | The payload of a log or run item, as text; `NULL` for a coalescing item |
| `due_at` | When a waiting item may be claimed |
| `again_due_at` | The mark "run again" of a coalescing item: the earliest due time that an enqueue asked for while the item was held. A retry or a release keeps it, so a waiting item can carry it too |
| `lease_until` | When a held item's lease ends; set exactly while the item is held |
| `fence` | Grows by one with every claim and is never reset |
| `attempts` | The claims since the item was added, revived or requeued, less those a release gave back |
| `enqueued_at` | When the enqueue added the item |
| `claimed_at` | When the item was last claimed |
| `finished_at` | When the item finished or died; set exactly for those two states |
| `reason` | The last failure |

Its indexes serve one operation each: the unique index on `queue` and `active_key`, where
`active_key` is set, for one item per key; the waiting items of a queue in the order of `due_at`
and `id`, for claims;
the held items of a queue by `lease_until`, for leases that ended; the finished and dead items of a
queue by `finished_at`, for retention.

`moltaro_work_scope` has a row exactly while an item of a scope is held, with `queue`, `scope` and
the held item's `item_id`. Its primary key on `queue` and `scope` is the scope rule, kept without
advisory locks.

`moltaro_work_schema` holds one row, the version of the schema. A later version comes with a
script that changes the tables and that row, and a store refuses, in `CheckDatabaseAsync`, a
database whose version is not its own.

Rules every provider keeps:

- **Exact comparison.** Queue names, keys and scopes compare exactly, case included: their columns
  use a binary collation. SQL Server's ignores trailing spaces, so a key or a scope may not end with
  one.
- **Times** are stored in UTC to the microsecond, truncated.
- **No foreign key**, because `READPAST` and skip-locked reads still wait on them.
- **Limits that the schema fixes.** A queue name is at most 100 characters, a key and a scope at
  most 200, as their columns fix; the core refuses a longer one. A reason is at most 4000
  characters, as SQL Server's `nvarchar(4000)` fixes it, while PostgreSQL's and MySQL's `reason` is
  `text`: the worker is to cut a longer reason to that length, never inside a surrogate pair,
  which is planned and not implemented yet. These limits change only with the schema's version.

## Reading the tables

There is no read API. The tables, their columns and the states above are part of the contract, so
an application reads them with its own SQL, read only: the history of its runs, or its dead items
with their reasons. It writes to them only through the store.

## PostgreSQL

- **Version.** PostgreSQL 15 or later, the oldest release supported on 26 November 2026.
- **Database.** Its encoding is UTF8. `CheckDatabaseAsync` checks it together with the schema
  version.
- **Script.** `src/Moltaro.Work.PostgreSql/Schema/version-1.sql`, which
  `PostgreSqlWorkStore.SchemaScript` returns. The application applies it once, with its own
  migration tool. The tables are created in the connection's default schema, the first of its
  `search_path`, where the store looks for them.
- **Isolation.** The store's own transactions run at READ COMMITTED, which it sets on each. The
  caller's transaction of an enqueue may run at any isolation.
- **Errors of an enqueue.** At REPEATABLE READ or SERIALIZABLE, an enqueue that meets a concurrent
  change of its key fails with `serialization_failure` (40001). At any isolation an enqueue may fail
  with `deadlock_detected` (40P01). Both reach the caller as a `PostgresException`, and the
  transaction is then aborted: it takes nothing but a rollback.

The partial indexes keep only the rows each operation reads, and the collation `"C"` compares
bytes:

```sql
-- Moltaro.Work schema version 1, PostgreSQL 15 or later.
CREATE TABLE moltaro_work_schema (
    version integer NOT NULL
);

INSERT INTO moltaro_work_schema (version) VALUES (1);

CREATE TABLE moltaro_work_item (
    id           bigint       GENERATED ALWAYS AS IDENTITY,
    queue        varchar(100) COLLATE "C" NOT NULL,
    state        smallint     NOT NULL,
    item_key     varchar(200) COLLATE "C",
    active_key   varchar(200) COLLATE "C",
    scope        varchar(200) COLLATE "C",
    payload      text,
    due_at       timestamptz  NOT NULL,
    again_due_at timestamptz,
    lease_until  timestamptz,
    fence        bigint       NOT NULL DEFAULT 0,
    attempts     integer      NOT NULL DEFAULT 0,
    enqueued_at  timestamptz  NOT NULL,
    claimed_at   timestamptz,
    finished_at  timestamptz,
    reason       text,
    CONSTRAINT moltaro_work_item_pk PRIMARY KEY (id),
    CONSTRAINT moltaro_work_item_state CHECK (state BETWEEN 1 AND 4),
    CONSTRAINT moltaro_work_item_lease CHECK ((state = 2) = (lease_until IS NOT NULL)),
    CONSTRAINT moltaro_work_item_finished CHECK ((state >= 3) = (finished_at IS NOT NULL))
);

CREATE UNIQUE INDEX moltaro_work_item_active_key ON moltaro_work_item (queue, active_key)
    WHERE active_key IS NOT NULL;
CREATE INDEX moltaro_work_item_waiting ON moltaro_work_item (queue, due_at, id) WHERE state = 1;
CREATE INDEX moltaro_work_item_held ON moltaro_work_item (queue, lease_until) WHERE state = 2;
CREATE INDEX moltaro_work_item_ended ON moltaro_work_item (queue, finished_at) WHERE state IN (3, 4);

CREATE TABLE moltaro_work_scope (
    queue   varchar(100) COLLATE "C" NOT NULL,
    scope   varchar(200) COLLATE "C" NOT NULL,
    item_id bigint       NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY (queue, scope)
);
```

## SQL Server

- **Version.** SQL Server 2019 or later, or Azure SQL Database.
- **Database.** READ_COMMITTED_SNAPSHOT is ON, because without it the reads of the candidates of a
  claim deadlock with claims. `CheckDatabaseAsync` checks it together with the schema version,
  before a worker runs. Claims read with `UPDLOCK, READPAST`, because under that setting a plain
  read does not skip locked rows.
- **Sessions.** Every session that changes the tables, the caller's included, needs the SET options
  of filtered indexes: ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT,
  CONCAT_NULL_YIELDS_NULL and QUOTED_IDENTIFIER ON, and NUMERIC_ROUNDABORT OFF. The defaults of
  Microsoft.Data.SqlClient meet them at a compatibility level of 90 or higher.
- **Script.** `src/Moltaro.Work.SqlServer/Schema/version-1.sql`, which
  `SqlServerWorkStore.SchemaScript` returns: one batch, without `GO`. The application applies it
  once, with its own migration tool. It sets the SET options itself, because `sqlcmd` leaves
  QUOTED_IDENTIFIER OFF. The tables are created in the connection's default schema, where the store
  looks for them. Lock escalation is off for the item table, so that a large claim or delete never
  locks every queue.
- **Isolation.** The store's own transactions run at READ COMMITTED. The caller's transaction of an
  enqueue may run at any isolation.
- **Errors of an enqueue.** At any isolation an enqueue may fail as a deadlock victim (1205), and
  under SNAPSHOT with an update conflict (3960). Both reach the caller as a `SqlException`, and SQL
  Server has then rolled the transaction back.
- **Cancellation.** Once the token of any of the store's operations is cancelled, a failure of the
  driver ends the task cancelled, with the driver's exception as its inner one.
  Microsoft.Data.SqlClient reports the cancellation of a command it sent as a `SqlException` of
  error number 0, "Operation cancelled by user.", and of a command not yet sent as an
  `InvalidOperationException`: that is the caller's cancellation, not a database error.

The filtered indexes stand for PostgreSQL's partial ones, and the checks are spelled out, because
T-SQL does not compare two conditions. `datetime2(6)` keeps microseconds, in UTC, and the collation
`Latin1_General_100_BIN2` compares code points:

```sql
-- Moltaro.Work schema version 1, SQL Server 2019 or later. Needs READ_COMMITTED_SNAPSHOT ON.
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

CREATE TABLE moltaro_work_schema (
    version int NOT NULL
);

INSERT INTO moltaro_work_schema (version) VALUES (1);

CREATE TABLE moltaro_work_item (
    id           bigint         IDENTITY(1, 1) NOT NULL,
    queue        varchar(100)   COLLATE Latin1_General_100_BIN2 NOT NULL,
    state        tinyint        NOT NULL,
    item_key     nvarchar(200)  COLLATE Latin1_General_100_BIN2 NULL,
    active_key   nvarchar(200)  COLLATE Latin1_General_100_BIN2 NULL,
    scope        nvarchar(200)  COLLATE Latin1_General_100_BIN2 NULL,
    payload      nvarchar(max)  NULL,
    due_at       datetime2(6)   NOT NULL,
    again_due_at datetime2(6)   NULL,
    lease_until  datetime2(6)   NULL,
    fence        bigint         NOT NULL CONSTRAINT moltaro_work_item_fence_default DEFAULT 0,
    attempts     int            NOT NULL CONSTRAINT moltaro_work_item_attempts_default DEFAULT 0,
    enqueued_at  datetime2(6)   NOT NULL,
    claimed_at   datetime2(6)   NULL,
    finished_at  datetime2(6)   NULL,
    reason       nvarchar(4000) NULL,
    CONSTRAINT moltaro_work_item_pk PRIMARY KEY CLUSTERED (id),
    CONSTRAINT moltaro_work_item_state CHECK (state BETWEEN 1 AND 4),
    CONSTRAINT moltaro_work_item_lease
        CHECK ((state = 2 AND lease_until IS NOT NULL) OR (state <> 2 AND lease_until IS NULL)),
    CONSTRAINT moltaro_work_item_finished
        CHECK ((state >= 3 AND finished_at IS NOT NULL) OR (state < 3 AND finished_at IS NULL))
);

CREATE UNIQUE INDEX moltaro_work_item_active_key ON moltaro_work_item (queue, active_key)
    WHERE active_key IS NOT NULL;
CREATE INDEX moltaro_work_item_waiting ON moltaro_work_item (queue, due_at, id) WHERE state = 1;
CREATE INDEX moltaro_work_item_held ON moltaro_work_item (queue, lease_until) WHERE state = 2;
CREATE INDEX moltaro_work_item_ended ON moltaro_work_item (queue, finished_at) WHERE state IN (3, 4);
ALTER TABLE moltaro_work_item SET (LOCK_ESCALATION = DISABLE);

CREATE TABLE moltaro_work_scope (
    queue   varchar(100)  COLLATE Latin1_General_100_BIN2 NOT NULL,
    scope   nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
    item_id bigint        NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY CLUSTERED (queue, scope)
);
```

## MySQL

- **Version.** MySQL 8.4 or later, with InnoDB tables and row-based binary logging, because reads
  that skip locked rows are unsafe for statement-based replication. MariaDB is not supported.
- **Script.** `src/Moltaro.Work.MySql/Schema/version-1.sql`, which `MySqlWorkStore.SchemaScript`
  returns. The application applies it once, with its own migration tool. MySQL commits every
  `CREATE TABLE` at once, also inside a transaction, so a script that fails halfway leaves the
  tables it created behind. The tables are created in the connection's database, its default
  schema, where the store looks for them.
- **Isolation.** The store's own transactions run at READ COMMITTED, which it sets on each. The
  caller's transaction of an enqueue may run at any isolation.
- **Errors of an enqueue.** At MySQL's default REPEATABLE READ, concurrent enqueues of one key in
  callers' transactions can deadlock (1213) or wait out `innodb_lock_wait_timeout` (1205). Both
  reach the caller as a `MySqlException`. After a deadlock MySQL has rolled the transaction back;
  after a lock wait timeout it has undone that statement alone, unless `innodb_rollback_on_timeout`
  is on.

MySQL has no partial index. A unique index over the nullable `active_key` admits any number of
`NULL`, which gives the same one item per active key. The waiting and held indexes lead with
`state`; the ended one needs none, because the checks set `finished_at` exactly for ended items.
`datetime(6)` keeps microseconds and holds UTC, because it has no time zone. The collations
`ascii_bin` and `utf8mb4_0900_bin` compare bytes and code points, and `utf8mb4_0900_bin` does not
pad:

```sql
-- Moltaro.Work schema version 1, MySQL 8.4 or later, InnoDB. Times are UTC.
CREATE TABLE moltaro_work_schema (
    version int NOT NULL
) ENGINE = InnoDB;

INSERT INTO moltaro_work_schema (version) VALUES (1);

CREATE TABLE moltaro_work_item (
    id           bigint       NOT NULL AUTO_INCREMENT,
    queue        varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    state        tinyint      NOT NULL,
    item_key     varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    active_key   varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    scope        varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    payload      longtext     CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    due_at       datetime(6)  NOT NULL,
    again_due_at datetime(6)  NULL,
    lease_until  datetime(6)  NULL,
    fence        bigint       NOT NULL DEFAULT 0,
    attempts     int          NOT NULL DEFAULT 0,
    enqueued_at  datetime(6)  NOT NULL,
    claimed_at   datetime(6)  NULL,
    finished_at  datetime(6)  NULL,
    reason       text         CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    CONSTRAINT moltaro_work_item_pk PRIMARY KEY (id),
    CONSTRAINT moltaro_work_item_state CHECK (state BETWEEN 1 AND 4),
    CONSTRAINT moltaro_work_item_lease CHECK ((state = 2) = (lease_until IS NOT NULL)),
    CONSTRAINT moltaro_work_item_finished CHECK ((state >= 3) = (finished_at IS NOT NULL)),
    UNIQUE INDEX moltaro_work_item_active_key (queue, active_key),
    INDEX moltaro_work_item_waiting (queue, state, due_at, id),
    INDEX moltaro_work_item_held (queue, state, lease_until),
    INDEX moltaro_work_item_ended (queue, finished_at)
) ENGINE = InnoDB;

CREATE TABLE moltaro_work_scope (
    queue   varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    scope   varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NOT NULL,
    item_id bigint       NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY (queue, scope)
) ENGINE = InnoDB;
```
