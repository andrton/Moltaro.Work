-- Moltaro.Work schema version 1, SQL Server 2019 or later. Needs READ_COMMITTED_SNAPSHOT ON.
-- The meaning of every table and column is in docs/storage.md. The tables are created in the connection's default
-- schema; the application applies this script once, with its own migration tool.

-- The SET options of filtered indexes, which every session that changes the tables needs. The script sets them itself,
-- because sqlcmd leaves QUOTED_IDENTIFIER OFF.
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

CREATE TABLE moltaro_work_schema (
    version int NOT NULL,
    CONSTRAINT moltaro_work_schema_pk PRIMARY KEY (version)
);

INSERT INTO moltaro_work_schema (version) VALUES (1);

-- state: 1 waiting, 2 held, 3 finished, 4 dead.
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

-- One item per active key: the one mechanism of coalescing and of the keys of logs and runs.
CREATE UNIQUE INDEX moltaro_work_item_active_key ON moltaro_work_item (queue, active_key)
    WHERE active_key IS NOT NULL;
CREATE INDEX moltaro_work_item_waiting ON moltaro_work_item (queue, due_at, id) WHERE state = 1;
CREATE INDEX moltaro_work_item_held ON moltaro_work_item (queue, lease_until) WHERE state = 2;
CREATE INDEX moltaro_work_item_ended ON moltaro_work_item (queue, finished_at) WHERE state IN (3, 4);

-- A large claim or delete locks its rows, never the whole table with every queue.
ALTER TABLE moltaro_work_item SET (LOCK_ESCALATION = DISABLE);

-- A row exists exactly while an item of its scope is held: its primary key is the scope rule.
CREATE TABLE moltaro_work_scope (
    queue   varchar(100)  COLLATE Latin1_General_100_BIN2 NOT NULL,
    scope   nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
    item_id bigint        NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY CLUSTERED (queue, scope)
);
