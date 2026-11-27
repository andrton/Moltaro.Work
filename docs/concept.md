# Moltaro.Work — concept

Status: agreed concept, 26 November 2026. Nothing is implemented yet;
[README.md](../README.md) says what works today.

## 1. Where it comes from

A business platform keeps its durable background work in its own database: jobs to run,
outbox messages to deliver, records to recompute, files to inspect, reminders that fall
due. Every feature that needs such work builds the same machinery again. In one platform of
a few dozen modules, a count found about forty hand-built queues and close to twenty
thousand lines of plumbing:
- each claims rows, leases them and fences their holder;
- each retries with a backoff and gives up after some attempts;
- each cleans up after itself.

The copies drift apart, and the differences are bugs:
- a queue that nobody wakes, so its new work waits for the next timer;
- a job that two workers process at once, because its lease has no heartbeat and its writes
  check only a status;
- one failing message that rolls back its whole batch, forever;
- a run that is retried without a limit;
- tables that nobody deletes from;
- retry state kept in memory and lost on restart.

Polling hides most of these, and costs a statement every few seconds on every queue while
nothing happens.

The observation behind this project: **all of that work has one of three shapes, and the
machinery is the same for all three.**
- An item may only say "this object needs attention", with no history.
- It may be one event that must be handled.
- It may be one run whose history someone reads later.

What differs is the handler. A library can own the rest, so that a feature declares its
work and writes its handler, and nothing else.

## 2. What it is

A .NET library for durable background work stored in the application's own relational
database.

- **A database-neutral core** keeps the rules: when an item may be claimed, how long its
  lease lasts, when it is retried, when it is given up, when it is deleted. It reaches the
  database only through ADO.NET (`DbConnection`, `DbTransaction`).
- **A provider package per database** holds the SQL of that database and the script of its
  schema. The first release ships providers for PostgreSQL, SQL Server and MySQL.
- **Enqueuing joins the caller's own transaction.** An item exists if and only if the
  business write that created it commits.
- **Workers** claim items in batches, run their handlers, and complete, retry or give up.
  They wake through [Moltaro.Signals](https://github.com/andrton/Moltaro.Signals) when new
  work is committed, and on their own at the earliest due time. There is no fixed polling
  interval.

It is not a message broker, a scheduler of cron expressions, a workflow engine or a
dashboard.

## 3. Semantics

### Terms

| Term | Meaning |
| --- | --- |
| Queue | A declared kind of work: its name, its shape, its handler and its policies |
| Item | One unit of work in a queue |
| Key | The identity of an item inside its queue, chosen by the application. Required for the coalescing shape; optional for the others, where it makes an enqueue idempotent |
| Scope | An optional second key of an item. At most one item per scope runs at a time |
| Due time | The earliest time an item may be claimed |
| Lease | The time a claimed item belongs to one worker |
| Fence | A token issued with each claim; every later write of that holder must present it |
| Attempt | One claim of an item |
| Dead item | An item given up after its last attempt, kept with its reason |
| Retention | How long finished and dead items are kept before they are deleted |

### Shapes

| Shape | One item per | Enqueue while one is waiting | Enqueue while one runs | After success |
| --- | --- | --- | --- | --- |
| **Coalescing** | Key | Nothing new; the due time becomes the earlier of the two while the item's attempts are at zero, and stays while it waits for a retry | The item runs once more after the current run | Deleted |
| **Log** | Enqueue | A new item | A new item | Deleted |
| **Run** | Enqueue, or key while active | A new item, or none while one with the same key is active | Same | Kept with its outcome for its retention |

- A coalescing item carries no payload: its key names the object, and the handler reads its
  current state.
- An enqueue of a dead coalescing item revives it: the item is waiting again, with its
  attempts counted from zero.
- A log item and a run carry a payload that the application serializes.
- A run keeps its status, attempts, outcome and times, so that an application can show its
  history.

### Rules

1. **Enqueue is transactional.** An item is written through the caller's connection and
   transaction, and exists only if that transaction commits.
2. **New work wakes a worker.** After the enqueuing transaction commits, the queue's signal
   is set through Moltaro.Signals. The application gives the library its commit in one
   call, or the integration of its data access layer does.
3. **Waiting is bounded without polling.** A worker waits for its queue's signal or until
   the earliest due time it knows, whichever comes first. When nothing is due it waits at
   most an idle bound, an option. That bound alone limits how long an item waits when the
   process that committed it died before setting the signal.
4. **A claim is exclusive.** A worker claims due items in a batch. Each claimed item is
   leased to it and carries a new fence. Two workers never hold the same item.
5. **Every write of a holder is fenced.** Heartbeat, completion, retry and giving up succeed
   only with the item's current fence. A holder whose lease was lost and reclaimed cannot
   complete the item.
6. **A lease is kept alive while the handler runs**, by a heartbeat. An item whose lease ends
   without a completion becomes due again, and that counts as an attempt.
7. **Failure is retried, then given up.** A handler that fails, or asks for a retry, makes its
   item due again after a backoff. After the queue's attempt limit the item becomes dead,
   with its reason. A dead item can be put back by an explicit requeue.
8. **Retry state lives in the item**, never in memory, so a restart loses nothing.
9. **At least once.** A handler may run more than once for an item, for example when its
   lease is lost after its effect. Handlers are written to be idempotent. The library never
   lets two holders complete the same item.
10. **Scope serializes.** In a queue that declares scopes, an item is not claimed while
    another item of its scope is held.
11. **Nothing grows forever.** Every queue has a retention for finished and dead items, and
    deletion runs in bounded batches.
12. **The database's clock decides.** Due times and lease ends are compared with the time of
    the database, so workers on machines with different clocks agree.

Exactly-once execution, ordering across scopes and priorities are not provided.

## 4. Providers

A provider implements the store operations of the core for one database:
- enqueue, including the coalescing upsert;
- claim a batch;
- heartbeat;
- complete, retry and give up, all fenced;
- requeue;
- the earliest due time;
- delete expired items in a batch.

It ships the schema of the core's tables as a versioned script; the application applies it
with its own migration tool.

Every provider passes one conformance suite against a real database in CI, including its
concurrency cases:
- many workers claiming at once;
- a worker that dies holding an item;
- a holder that writes after its lease was reclaimed.

Each provider writes the best SQL of its database:
- the skip-locked claim (`FOR UPDATE SKIP LOCKED`, or `READPAST` in SQL Server);
- a one-statement claim where the database has `RETURNING` or `OUTPUT`;
- a partial or filtered index where the database has one.

A provider names the isolation its database needs, for example READ COMMITTED SNAPSHOT
in SQL Server and READ COMMITTED in MySQL.

The scope rule and the fence use plain tables and unique constraints, never a database's
advisory locks, so that every provider can keep them.

## 5. Hosting

A hosting package registers queues, handlers and workers in a .NET generic host and
connects them to a Moltaro.Signals client or host. A worker runs a number of lanes per
queue, an option. It stops gracefully: claimed items are completed or released when the host
stops.

## 6. Decisions

| # | Decision | Why |
| --- | --- | --- |
| D1 | Work is stored in the application's own database and enqueued in its transaction, through ADO.NET | The item commits or rolls back with the write that created it; no outbox in front of the queue |
| D2 | Three shapes: coalescing, log and run, on one mechanism | They cover the work a platform has; the machinery is the same |
| D3 | At least once, with fencing; no exactly-once | Exactly-once needs the handler's effects in the same transaction, which a general library cannot own |
| D4 | The database's clock decides due times and leases | Workers on different machines agree without synchronized clocks |
| D5 | Retry state, attempt limit and dead items live in the item; requeue is explicit | A restart loses nothing, and a poison item stops retrying |
| D6 | Every queue has a retention, deleted in bounded batches | Nothing grows forever |
| D7 | Workers wake through Moltaro.Signals and at the earliest due time, with an idle bound; no fixed polling | New work starts at once and an idle database is left alone |
| D8 | A database-neutral core with a provider package per database: PostgreSQL, SQL Server and MySQL in the first release, held by one conformance suite | SQL has no standard claim, so neutrality means a provider per database; .NET applications on any of the three can use the library |
| D9 | No advisory locks; the scope rule and fences use tables and unique constraints | Every provider can keep them in the same way |
| D10 | The core's decisions are a pure component, tested deterministically with a fake clock | Correctness does not depend on a database to test |
| D11 | Each provider ships its schema as a versioned script; the application applies it with its own migration tool | The library does not impose a migration framework |
| D12 | A hosting package for the .NET generic host | Registration in a few lines; the core stays free of hosting |
| D13 | .NET 10, Apache-2.0, a public GitHub repository, English documentation | The same as its sibling projects |
| D14 | Versions stay 0.x until a first production adopter runs on the library | Freedom to fix the design before promising compatibility |
| D15 | Packages on nuget.org, published by GitHub Actions from a version tag through Trusted Publishing; releases are regular versions | Free, no token to restore, no stored secret |
| D16 | KISS: the scope is durable background work; requests outside it are declined or noted as planned | The value is a small library a team can read and trust |

## 7. Non-goals and possible later work

**Not in the first release:**
- a message broker or delivery of one item to many consumers;
- exactly-once execution;
- priorities and ordering across scopes;
- cron expressions;
- workflows and sagas;
- a dashboard;
- providers for databases other than the three;
- a provider for a non-relational store.

**Possibly later, only with a real consumer:**
- a provider for another database or for an in-memory durable store;
- recurring work, with a handler that enqueues its next occurrence;
- an administration API for dead items beyond requeue.

## 8. Existing tools

| Tool | Why not |
| --- | --- |
| River (Go), Oban (Elixir), graphile-worker (Node.js) | Close in spirit, and a source of ideas; not .NET |
| Hangfire | Its own job model and storage abstraction, serialized method calls, polling; no coalescing per key |
| Quartz.NET | A scheduler of triggers, not a queue of work items |
| Wolverine, MassTransit (SQL transport), DotNetCore.CAP | Messaging frameworks with durable inboxes and outboxes; a message model, not coalescing work items, and much more than this |

## 9. Quality bar

**Tests:**
- every rule of section 3 has a deterministic test, of the pure component where .NET decides
  and of the conformance suite where SQL decides, and rule 12 is held by the review of the
  SQL;
- every provider passes the conformance suite on its real database in CI;
- no test sleeps; time is injected through `TimeProvider`.

**Performance targets** (measured by a measurement program, not gating tests):
- an item that falls due starts within one second;
- new work starts within a wake-up;
- statements per processed item, for claim and completion in a batch, are reported per
  provider;
- an idle queue sends no statement between its due times.

**Observability.** `System.Diagnostics.Metrics` per queue:
- depth;
- the age of the oldest due item;
- claims, completions, retries and dead items.
