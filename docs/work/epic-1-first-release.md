# Epic 1: First release

**Status:** planned, 27 November 2026; waiting for the owner's approval of this plan and,
with it, of the contract of Stage 1. No sprint has started.

## Standing permission

On 26 November 2026 the owner gave the coordinator, for this epic, the standing permission
that [branches and releases](branches.md#who-does-what), "Who does what", describes: for a
closed sprint with no decision of the owner open, the coordinator pushes its branch, opens
the pull request and squashes it into `main` once every CI job has passed. A decision of
the owner is open, and holds a merge, when the coordinator has stopped and asked for it, or
when an open question names the sprint being merged or an earlier one as its limit; a
change listed under [Changes of the contract](#changes-of-the-contract) holds none, because
the contract is a draft until the release. It never covers a release tag, a force-push,
applying a ruleset or changing a setting of the repository.

## Goal

The library as the [concept](../concept.md) describes it, published on nuget.org as
version 0.1.0: the core `Moltaro.Work`, the providers `Moltaro.Work.PostgreSql`,
`Moltaro.Work.SqlServer` and `Moltaro.Work.MySql`, and `Moltaro.Work.Hosting`.

It is for .NET applications that keep their background work in their own relational
database: a feature declares a queue, enqueues items in the transaction of the write that
creates them, and writes a handler. The library claims, leases, fences, retries, gives up,
deletes and wakes, the same way on all three databases.

## Scope

**Included.** Everything sections 2 to 5 and 9 of the concept describe: the three shapes,
rules 1–12, the provider contract and three providers with their versioned schema scripts,
one conformance suite on the real databases in CI, the pure decision component, the
hosting package, the samples, the metrics, the measurement program, the explanations and
the release.

**Left out**, as concept §7 says: a message broker, exactly-once execution, priorities and
ordering across scopes, cron expressions, workflows, a dashboard, other databases, a
non-relational store, recurring work, an administration API beyond requeue. Also left out
because nothing needs them now:
- an integration with a data access layer such as Entity Framework Core;
- a read API for items and runs ([PQ4](#pq4-reading-runs-and-dead-items));
- binding options from `IConfiguration`, health checks, logging inside the core;
- configurable table names: the tables live in the connection's default schema;
- a migration runner: the application applies the script with its own tool (D11).

## The study

The study of 26 November 2026 answered how established work queues claim, lease, fence,
retry, give up, clean up and wake, and what SQL each database needs. Its report is not in
the repository; the briefs state the facts they need. Every source was pinned and its
licence read first, by the [rule on sources](process.md#code-modelled-on-another-project).

| Source | Licence | Tag | Commit | Read |
| --- | --- | --- | --- | --- |
| Oban (Elixir) | Apache-2.0 | v2.24.0 | `a315b6dd777ae0abfbbd5c2b1aed21f799080f57` | Source |
| graphile-worker (Node.js) | MIT | v0.18.0 | `4cda192c5df254392a1dff350e5d73f7d2c18a85` | Source |
| Quartz.NET (ADO.NET job store) | Apache-2.0 | v4.8.1 | `10bf35e7409cab54bdcad05a8cf7732d47e5ab5f` | Source |
| River (Go) | MPL-2.0 | v0.51.2 | `8468ff25e2cc7afd790d7037703ae72d8b2138cb` | Documentation only, until the owner answers [Q1](open-questions.md#q1-may-the-source-of-a-project-under-mpl-20-be-read) |
| Hangfire (.NET) | LGPL-3.0 or commercial | v1.8.25 | `1d4778c23410b365b5f9ce35458202efe0b0f3ea` | Documentation only |

**What the plan takes**, as ideas; no code is adapted from any source, so
`THIRD-PARTY-NOTICES.md` stays as it is:
- from graphile-worker and Oban: the skip-locked claim that stamps the holder and counts
  the attempt in the same statement, with the candidates in a CTE so that the limit holds;
  the attempt counted at claim, so that a lost lease costs an attempt (rule 6);
- from graphile-worker: every comparison on the database's clock, partial indexes over
  waiting items, the upsert on the key for coalescing, and a plain table with row locks
  instead of advisory locks for the scope rule, with at most one item per scope in a batch;
- from River's documentation: a partial unique index for "one active item per key";
- from Quartz.NET: a claim token, where a write that matches no row is refused, not
  retried; READ COMMITTED SNAPSHOT on SQL Server with explicit `UPDLOCK, READPAST`, because
  without it candidate reads deadlock with claims;
- from Oban, retention deleted in batches with a row limit; from Hangfire's documentation,
  a lease that its holder keeps renewing.

**A design of our own**, where no source fits: the fence per claim, and the lease with a
heartbeat and a reclaim inside the claim (S6); coalescing while held (S5); scope without
advisory locks (S11); waking with no idle statement (S7); the enqueue under the caller's
isolation and its errors (S5). [Where the concept is silent](#where-the-concept-is-silent)
and the briefs' acceptance fix what each must keep; the stage's contract decides how.
Polling, leader-elected maintenance, completion by id alone and advisory-lock uniqueness
were rejected.

## Requirements

Levels are those of the [work README](README.md); in this epic **Release** is part of the
acceptance. "Today" is the state on 26 November 2026: no code exists.

| # | Requirement | From | Level | Today | Sprints |
| --- | --- | --- | --- | --- | --- |
| R1 | An item is written through the caller's `DbTransaction` and exists only if it commits; the library opens no transaction around the caller's write | Rule 1, D1, principle 1 | Must | Absent | S1, S5, S10, S14, S15 |
| R2 | The application commits through the library in one call, which then sets the signal of every queue it enqueued into | Rule 2 | Must | Absent | S1, S5 |
| R3 | The three shapes, as the concept's table of shapes and W2–W4 say | Concept §3, D2 | Must | Absent | S1, S5, S10 |
| R4 | A claim takes due items in a batch; each claimed item gets a new fence; two holders never hold an item; every holder write presents the fence, and a refused one is reported | Rules 4, 5, 9, D3 | Must | Absent | S6, S9, S14, S15 |
| R5 | A heartbeat keeps a lease; an item whose lease ends without a completion is due again, and that was an attempt | Rule 6 | Must | Absent | S6, S7 |
| R6 | Failure and a requested retry make the item due after a backoff; after the attempt limit it is dead with its reason; an explicit requeue puts it back; all of it lives in the item | Rules 7, 8, D5 | Must | Absent | S9 |
| R7 | In a queue that declares scopes, no item is claimed while another of its scope is held, without advisory locks | Rule 10, D9 | Must | Absent | S11 |
| R8 | Retention per queue, deleted in bounded batches, never an item that is not finished or dead | Rule 11, D6 | Must | Absent | S12 |
| R9 | Due times and lease ends are compared with the database's time, and so is the deadline a worker waits for (W6) | Rule 12, D4 | Must | Absent | S5–S7, S12 |
| R10 | A worker waits for its signal, its earliest due time or its idle bound; no statement while nothing is due | Rule 3, D7, principle 5 | Must | Absent | S7, S12 |
| R11 | Every observable timeout and limit is an option with a default, validated when its queue or worker is created | Principle 10 | Must | Absent | S1 |
| R12 | Distinct results: added or coalesced, a write accepted or refused by the fence, requeued or not; never an invented success | Principle 11 | Must | Absent | S1, S5, S6, S9 |
| R13 | The provider contract, and one provider per database with its best SQL, its versioned schema script, its minimum version and the isolation it needs | Concept §4, D8, D11 | Must | Absent | S1–S3, S5–S12, S14, S15 |
| R14 | One conformance suite, written once against the provider contract, run by each provider's test project on its real database in CI, with the concurrency cases of `AGENTS.md` | Concept §4, §9; `AGENTS.md` | Must | Absent | S2, S3, S5–S12, S14, S15 |
| R15 | A pure decision component with deterministic tests; every rule of concept §3 held by a deterministic test as PQ3 decides | D10, principle 8, concept §9 | Must | Absent | S7, S9, S12 |
| R16 | The hosting package: a store and workers in a .NET generic host, connected to a Moltaro.Signals `ISignals`; lanes; graceful stop; log lines | Concept §5, D12 | Must | Absent | S4, S8 |
| R17 | Samples of intended use that run to the end with `dotnet run` | `AGENTS.md` | Must | Absent | S1, S2, S4, later sprints |
| R18 | Metrics per queue through `System.Diagnostics.Metrics`: depth, age of the oldest due item, claims, completions, retries, dead items | Concept §9 | Release | Absent | S13 |
| R19 | The measurement program and the figures of concept §9 per provider | Concept §9, principle 13 | Release | Absent | S16 |
| R20 | The explanations of S17, as Markdown and as HTML | `AGENTS.md`, "Documentation" | Release | Absent | S17 |
| R21 | Five packages, each depending only on what principle 12 allows, published together from a version tag through Trusted Publishing | D13, D15, principle 12 | Release | The workflows exist; no package | S1–S4, S18 |
| R22 | `README.md`, the concept, `docs/storage.md` and the explanations say what exists after every sprint | `AGENTS.md`, "Documentation" | Release | Holds: nothing exists and they say so | Every sprint |

## Acceptance

The epic is done when all of the following hold.

1. Every requirement above is delivered. No entry point throws `NotSupportedException`
   and no test is skipped.
2. The tests pass with the assertions they were committed with, except corrections of a
   wrong test made by the rule of the [development
   process](process.md#a-sprint-on-the-full-track).
3. The public surface is the contract of this plan with the changes listed under
   [Changes of the contract](#changes-of-the-contract), and nothing else.
4. From `src`, `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass on
   the `build` job, with PostgreSQL, SQL Server and MySQL as service containers; the
   `windows` job builds and runs the test projects that need no database. The samples run
   to the end; the code in `README.md` compiles against the contract.
5. The figures of concept §9 are recorded per provider in `README.md`, and each target is
   met or its miss was handled by the rule of the
   [development process](process.md#when-to-stop-and-ask-the-owner).
6. The explanations exist in both forms and agree with each other and with the code.
7. `dotnet pack` from `src` produces exactly the five packages, and S18 is done, so that
   the owner can tag `v0.1.0`.

### Changes of the contract

Until the first release the contract is a draft ([process](process.md#the-plan)): a sprint
that changes it makes the change in its branch and lists it here for the owner, with the
sprint and the reason. None yet.

## Open questions

Each question changes a guarantee, a decision or a rule, so it is the owner's. The contract
of Stage 1 is written with the recommended answer; another answer changes the brief named.

### PQ1. What an enqueue does to a dead coalescing item

The concept says what an enqueue does to a coalescing item that waits or runs, not to one
that is dead. One row per key holds in every state
([S2](#s2--the-postgresql-provider-contract-and-schema)).
- **(a) It revives the item:** the item becomes waiting with the due time of the enqueue
  and its attempts counted from zero; its last reason stays until the next failure. An
  enqueue while the item is held and ends dead does the same when it gives up.
- **(b) It leaves the item dead:** the enqueue writes nothing and reports a status of its
  own, `Dead`; only a requeue, or retention, ends the dead item.

**Recommended: (a).** A coalescing item says "this object needs attention" and its handler
reads the current state, so after a fix of the data or the code the next write processes
the object again; a poison key costs at most the attempt limit per write. Under (b) every
later write waits for a requeue. Briefs: S1, S2, S5, S9.

### PQ2. Whether a log item can be kept after success

The concept's table says a log item is "deleted, or kept for its retention" after success,
and a run is "kept with its outcome for its retention". A log item that is kept behaves as
a run: kept with its state, attempts and times.
- **(a) No option:** log items are deleted after success; an application that wants the
  history declares a run queue. The concept's cell becomes "Deleted".
- **(b) An option `WorkQueueOptions.KeepFinished`,** valid for a log queue only.

**Recommended: (a)**: one shape for one need, and no option whose meaning depends on the
shape. Briefs: S1, S10; with (b), `KeepFinished` joins S1's contract.

### PQ3. Where each rule of section 3 is tested

Concept §9 and `AGENTS.md` say that every rule of §3 has a deterministic test of the pure
component. Principle 8 names "what may be claimed" among its decisions, while principle 9
and rule 12 put every comparison of due times and lease ends into SQL with the database's
clock.
- **(a)** The decision component holds what the worker decides in .NET: run or give up after
  a claim, complete, retry with which delay or give up after a handler, how long to wait,
  when a lease counts as lost. A rule decided by SQL on the database's clock — claimability,
  lease ends, fences, scopes, expiry, the shapes' upserts — is held by a deterministic case
  of the conformance suite, which moves the database's clock for its queue instead of
  waiting ([S2](#s2--the-postgresql-provider-contract-and-schema)). Rule 12 itself is held
  by the review of the SQL, because the tests and the database share one clock. The plan
  maps each rule to its tests ([Rules and their tests](#rules-and-their-tests)).
- **(b)** The decision component also states every SQL predicate as a pure function, and
  the conformance suite checks each provider against it.

**Recommended: (a)**: under (b) every predicate exists twice, once in C# that production
never calls. The sentence of
§9 and of `AGENTS.md` would read "a deterministic test, of the pure component where .NET
decides and of the conformance suite where SQL decides"; S1 makes the edit as the owner
words it.

### PQ4. Reading runs and dead items

A run is kept "so that an application can show its history"; a dead item is requeued by
its id. The concept names no read operation among the provider's operations (§4).
- **(a)** No read API in 0.1.0. The tables, columns and states are part of the contract in
  `docs/storage.md`, and an application reads them with its own SQL, read only.
- **(b)** A read API, for example `WorkClient.GetItemAsync(queue, id)`, as a new provider
  operation.

**Recommended: (a)**, as concept §7 leaves "an administration API for dead items beyond
requeue" for later. Briefs: S2, S3, S10.

### PQ5. The connection strings of the CI service containers

`AGENTS.md` says no connection string goes into Git. The `build` job of CI and the job of
Publish, which runs the same tests before it packs, need connection strings to their
PostgreSQL, SQL Server and MySQL service containers, whose passwords exist only inside the
job.
- **(a)** The owner approves exception X1: `ci.yml` and `publish.yml` name the connection
  strings of their own service containers, with throwaway passwords, and nothing else in
  the repository does.
- **(b)** The passwords are repository secrets, which D15 avoids for publishing.

**Recommended: (a)**; a secret would protect nothing. The SQL Server image runs only with
`ACCEPT_EULA=Y`, which accepts Microsoft's licence: the owner accepts it with this answer.
S2 records X1 in [exceptions to the rules](exceptions.md), in force from S2.

### PQ6. Where the conformance suite lives

`AGENTS.md` puts code that several test projects share into `Moltaro.Work.TestSupport`,
which "contains no tests", and says the conformance suite is "shared by the provider test
projects".
- **(a)** The suite is a set of abstract test classes in `Moltaro.Work.TestSupport`, folder
  `Conformance`. TestSupport is no test project and runs nothing; the suite runs only in
  the classes each provider's test project derives from it.
- **(b)** A project `Moltaro.Work.Conformance` of its own, not listed in "Layout".

**Recommended: (a)**: no new project, and the rule's purpose — nothing runs in TestSupport —
holds. Briefs: S1, which edits that sentence of `AGENTS.md` as the owner words it; S2.

### PQ7. What a stop does to the attempt that runs

Concept §5 says claimed items are "completed or released when the host stops"; rule 6
counts a lost lease as an attempt, and the attempt is counted at claim.
- **(a)** A release counts the attempt, as a lost lease does: at the attempt limit the item
  is given up, so an item that never failed can die of deploys.
- **(b)** A release gives the attempt back: the item is waiting at once with the attempts it
  had before the claim, through a fenced `IWorkStore.ReleaseAsync`.

**Recommended: (b)**: the worker knows that the stop, not the item, ended the attempt, and
"released" says no more. A handler that hangs gains nothing from (b): its heartbeats keep it
held either way. Briefs: S1 (`ReleaseAsync`), S9.

### PQ8. An enqueue while a coalescing item waits out its backoff

The table of shapes moves the due time of a waiting item to the earlier of the two; rule 7
makes a failed item due "after a backoff". They disagree for an item that waits for a retry.
- **(a)** The table wins: every enqueue can end a backoff, so during an outage a hot key uses
  up its attempts in seconds.
- **(b)** Rule 7 wins: an enqueue moves the due time only of an item that has not failed
  since it was added, revived or requeued; one that waits for a retry keeps its due time,
  and the enqueue reports `Coalesced`.

**Recommended: (b)**: the backoff protects what fails, and nothing is lost, because the
retry reads the current state. Briefs: S2 (`docs/storage.md`), S5, S6.

## Where the concept is silent

What follows from a rule that already stands, decided by the planner and reported to the
owner with this plan. Briefs point to these entries; the contract of Stage 1 carries them.

- **W1. Identity.** An item is identified by a `long` that the database assigns. A claim is
  named by the item's id and its fence: the fence grows with every claim and is never
  reset, so the pair never repeats.
- **W2. Keys of logs and runs.** A key of a log or run item is unique among the queue's
  active items, those waiting or held: the run row of the concept's table, applied to logs,
  whose idempotent enqueue the concept names without another rule. An enqueue whose key an
  active item has writes nothing and reports `Coalesced` with that item's id.
- **W3. Coalescing.** One row per key in every state. An enqueue while the item waits sets
  its due time to the earlier of the two, as the concept says, unless it waits for a retry
  (PQ8). An enqueue while it is held sets a mark "run again" with the earliest due time any
  such enqueue asked for. The mark is applied when the attempt ends: a completion makes the
  item waiting again at that time, with attempts counted from zero; giving up does as PQ1
  says; a retry keeps the mark. The scope of a coalescing item is the one of the enqueue
  that added it; a later enqueue does not change it.
- **W4. Payload.** A `string` that the application serializes, usually JSON, stored as text
  in every provider, so that a history can be read in SQL. A coalescing item has none. A
  payload may not contain U+0000, which PostgreSQL cannot store, nor an unpaired surrogate,
  which UTF-8 cannot, so that every provider accepts the same payloads.
- **W5. Due time.** An enqueue may name a `DateTimeOffset` as its due time, compared with
  the database's clock. `null`, or an instant already passed by that clock, means at once.
  Times are stored in UTC to the microsecond, truncated.
- **W6. The deadline of a wait.** The provider returns durations computed in SQL: from the
  database's now to the next claimable time and to the next expiry. The worker adds the
  shorter one, bounded by its idle bound, to the now of its `TimeProvider`, and passes that
  instant as the `until` of `ISignalSubscription.WaitAsync`. A duration compares no two
  clocks, so rule 12 holds; the error is the drift of one clock during one wait. The
  worker's `TimeProvider` is therefore the one of its `ISignals`: `TimeProvider.System`
  for both in production, the same fake in tests.
- **W7. Lease ends are not wake-ups.** A worker does not wait for lease ends, which
  heartbeats keep moving, so that no member wakes for nothing. An item whose holder died is
  claimed again by the next claim of any worker after its lease ended, at the latest at an
  idle bound, as rule 3 bounds a lost signal; `UntilClaimable` is zero once a lease has ended
  and is never a future lease end. A claim or a delete that takes nothing although the
  database said something was due waits for the signal or the idle bound, not for that
  time: the row is locked by a transaction or a holder whose end wakes a worker (W16), and
  the idle bound covers a rollback.
- **W8. Requeue.** A requeue makes a dead item waiting at once, with attempts counted from
  zero; its last reason stays until the next failure. It runs on the store's own connection
  in a transaction of its own, then sets the queue's signal. It is refused with a status of
  its own when the item is not dead, and, for a log or run item with a key, when another
  active item has that key, also one enqueued at the same moment: never as the database's
  unique violation.
- **W9. Stop.** When the worker stops, it cancels the token of every running handler and
  waits for it. An attempt that ends after the stop began, with anything but `Completed`,
  is released as PQ7 says. After the stop each outcome is written once; a write that fails
  leaves its item to its lease (rule 6).
- **W10. Errors of an enqueue.** The upsert runs in the caller's transaction at the caller's
  isolation. A provider neither catches nor retries a database error there: the driver's
  `DbException` reaches the caller through the task of `EnqueueAsync`, and the transaction
  is as its database leaves it: rolled back, aborted, or with that statement alone undone.
  The caller rolls back and retries its transaction, as for any write. Each provider
  documents which errors its enqueue can raise at which isolation.
- **W11. Signals.** A worker of queue `q` is a member of group `moltaro.work` on the key
  `moltaro.work.q`, and a commit sets that key. A queue name has no `.`, so a set never
  reaches another queue's key as an ancestor.
- **W12. Failures of the worker itself.** A statement of the worker that fails — the
  database is unreachable, say — is reported through `WorkWorker.Failed`, and the worker
  tries again after `WorkWorkerOptions.FailureDelay`; it never stops on a database error.
- **W13. Handler outcomes.** A handler returns `Completed` or `Retry`; an exception is a
  failure. The reason kept with a failed attempt is the exception's `ToString()`, cut to at
  most 4000 characters and never inside a surrogate pair; for `Retry` it is a fixed sentence.
- **W14. Batch size.** A claim asks for as many items as the worker has free lanes, so the
  batch size is `WorkWorkerOptions.Lanes` and needs no option of its own.
- **W15. Exact keys.** Queue names, keys and scopes compare exactly, case included, in every
  provider: their columns use a binary collation. A key or scope may not end with a space,
  because SQL Server ignores trailing spaces when it compares.
- **W16. Handing on.** A set wakes one member of a group (W11), so a worker that knows of
  claimable work it does not take sets the queue's key itself: after a claim that filled its
  free lanes, when a due time it knows is reached while no lane is free, and after a write
  of its own that makes an item waiting at once — a completion that applies a mark, a
  release, the end of a hold in a scoped queue. After a write that makes an item waiting
  later, its next deadline is at most that delay. A worker woken for nothing costs one claim.

### Rules and their tests

How each rule of concept §3 is held, by PQ3 (a). "Policy" is the decision component,
tested in `Moltaro.Work.Tests`; "conformance" is the suite on each real database.

| Rule | Held by | Sprint |
| --- | --- | --- |
| 1. Enqueue is transactional | Conformance: a rolled-back enqueue leaves nothing; a committed one is claimable | S5 |
| 2. New work wakes a worker | Conformance, worker: a commit starts the handler without moving any clock | S7 |
| 3. Waiting is bounded without polling | Policy: the wait is the shorter of the next due time and the idle bound; conformance, worker: no statement while idle, none for a locked due row (W7), two workers share a burst (W16) | S7 |
| 4. A claim is exclusive | Conformance: many claimers at once never return one item twice | S6 |
| 5. Every write of a holder is fenced | Conformance: every write with a reclaimed fence is refused | S6 |
| 6. A lease is kept alive | Conformance: a heartbeat moves the lease; an ended lease makes the item claimable and counts the attempt; policy: when a lease counts as lost | S6, S7 |
| 7. Retried, then given up | Policy: delay of each attempt, give up after the limit; conformance: retry, dead with reason, requeue | S9 |
| 8. Retry state lives in the item | Conformance: a new store and worker continue the attempts | S9 |
| 9. At least once | Conformance: of two holders of one item, only the current one completes | S6 |
| 10. Scope serializes | Conformance: no claim while an item of the scope is held, also under concurrent claimers | S11 |
| 11. Nothing grows forever | Conformance: bounded batches, never an item that is not finished or dead; policy: the wait includes the next expiry | S12 |
| 12. The database's clock decides | Review of the SQL: every comparison of a due time or a lease end uses the database's time function, and no provider sends a time of the worker | S5, S6, S14, S15 |

## Sprints

### Rules for every sprint

- **The process.** The [development process](process.md) applies. A sprint on the full
  track is three commits by one implementer — contract, tests, implementation — then the
  fixes; a sprint of Stage 1 implements only the checks of its contract, which are written
  in its contract commit, so its third commit is empty and is not made.
- **Branches.** As [branches and releases](branches.md) says: `epic-1/sprint-<nn>-<name>`
  on the full and the refactoring track, `docs/` or `chore/` on the light one.
- **Models and effort.** Every agent is Opus 5.5 (`claude-opus-5-5`), at the effort of
  [Models and effort](process.md#models-and-effort); each brief names its implementer's.
- **The architect's review** follows every merged sprint, as the first task of the next
  sprint's coordinator; after the last sprint of a stage it reads the whole code base. A
  refactoring sprint, named after its sprint (S7R after S7), exists only when the
  architect's list has an entry that was taken; it is on the refactoring track, with a
  refactorer at `high`, and its brief says to the refactorer that KISS holds there as
  everywhere.
- **Stages and their contracts.** The contract of Stage 1 is written out below and is
  approved with this plan. For a later stage the briefs say what the sprint must achieve and
  the cases that prove it, not how its SQL or its loop is built; before the stage begins,
  the planner goes through the [open questions](open-questions.md), writes the internal
  types and any public change into its briefs, an independent reviewer reviews them, and the
  owner approves the public part. The coordinator approves internal contracts.
- **To read, for every sprint:** `AGENTS.md`, the [concept](../concept.md), the
  [development process](process.md) and [branches and releases](branches.md), at their
  state of 26 November 2026; this plan as the owner approved it, with
  [The study](#the-study), [Where the concept is silent](#where-the-concept-is-silent) and
  the owner's answers to its questions; `docs/storage.md` as the last sprint left it. A
  brief names what it needs beyond these.
- **Tests.** xUnit.net v3 (`xunit.v3`) on Microsoft.Testing.Platform, as in Moltaro.Signals;
  S1 names the runner in `src/global.json`. The fake clock is `FakeTimeProvider` of
  `Microsoft.Extensions.TimeProvider.Testing`. A test waits for an observed event and never
  sleeps. A `SignalHost` of a test or a sample listens on the loopback address, port 0, with
  a token generated at run time. A test of a behavior that is not implemented yet is skipped
  with the one shared reason, `"Not implemented yet."`, a constant of
  `Moltaro.Work.TestSupport`, which S2 creates; S1 skips nothing. A folder of a test project
  is one area of behavior, so that a sprint un-skips whole folders.
- **Databases.** A provider's test project needs its database and fails, never skips,
  without it. It reads a connection string from `MOLTARO_WORK_POSTGRESQL`,
  `MOLTARO_WORK_SQLSERVER` or `MOLTARO_WORK_MYSQL`, to a server where it may create
  databases; each run creates a database of its own, applies the provider's schema script
  and drops it at the end. Every test uses a queue of its own name, so tests share the
  database. A test moves the database's clock for its queue, never waits for it: the
  provider's test project shifts every time of the queue's items back through one statement
  of its own. Locally the databases run in containers of the images CI uses; no connection
  string is written into the repository except as PQ5 decides. The `windows` job of CI runs
  only the test projects that need no database.
- **Samples.** `Moltaro.Work.Samples` runs every sample in order with `dotnet run` and ends.
  From S2 the samples use PostgreSQL through `MOLTARO_WORK_POSTGRESQL`; started without it,
  the program says which variable to set and runs only the samples that need no database. A
  sample that reaches a member not implemented yet catches its `NotSupportedException`,
  prints `Stops here: <what> is not implemented yet.`, and the program goes on with the next
  sample. A sprint that makes a behavior work makes its sample run further.
- **Documents.** In the documents step the coordinator brings `README.md`, the concept,
  `docs/storage.md` and the explanations up to what exists, as `AGENTS.md` and the process
  say. The concept is edited only as an answer of the owner says; S1 makes the edits that
  the owner's answers to this plan's questions require in the concept and in `AGENTS.md`,
  as the owner words them.

### Order

```text
Stage 1 (contract)      S1 core ── S2 PostgreSQL schema ── S3 SQL Server and MySQL schemas ── S4 hosting, samples
Stage 2 (thinnest path) S5 coalescing enqueue ── S6 claim, lease, fence ── S7 worker ── S8 hosting
Stage 3 (widen)         S9 retries ── S10 logs and runs ── S11 scopes ── S12 retention ── S13 metrics
Stage 4 (providers)     S14 SQL Server ── S15 MySQL
Stage 5 (release)       S16 measurements ── S17 explanations ── S18 release 0.1.0
```

Stage 1 is split by package, each schema with its provider, so that a schema is reviewed
with the facts of its database. Stage 2 is split by zone: the write in the caller's
transaction, the claim and its fence, the loop on top of them. Stage 3 widens on PostgreSQL
alone, so that each mechanism is designed and reviewed once before Stage 4 writes it for
two more databases against a finished conformance suite.

### Status

| Sprint | Result | Track | Status |
| --- | --- | --- | --- |
| S1 | The core contract and the test lane | Full | Not started |
| S2 | The PostgreSQL provider contract and schema | Full | Not started |
| S3 | The SQL Server and MySQL provider contracts and schemas | Full | Not started |
| S4 | The hosting contract and the samples | Full | Not started |
| S5 | Coalescing enqueue and the commit that wakes, on PostgreSQL | Full | Not started |
| S6 | Claim, lease, heartbeat and fenced completion | Full | Not started |
| S7 | The worker: lanes, waking and stop | Full | Not started |
| S8 | The hosting package works | Full | Not started |
| S9 | Retries, dead items, release and requeue | Full | Not started |
| S10 | Logs and runs | Full | Not started |
| S11 | Scopes | Full | Not started |
| S12 | Retention | Full | Not started |
| S13 | Metrics | Full | Not started |
| S14 | The SQL Server provider | Full | Not started |
| S15 | The MySQL provider | Full | Not started |
| S16 | The measurements | Light | Not started |
| S17 | The explanations | Light | Not started |
| S18 | The release of 0.1.0 | Light | Not started |

A sprint of Stages 2–5 starts only once its stage's contract is approved. An architect's
review follows each sprint once it is merged.

## Stage 1: the public contract

Approved with this plan. Its four sprints create the five packages and the whole public
.NET contract with its documentation, the schema of each database, the samples, and the
tests of every check that needs no implementation. Every other entry point throws
`NotSupportedException` until the sprint named with it. A name, a signature or a behavior
that is not written here goes back to the planner.

### Rules for every public member

Every member's XML documentation states the rules below that apply to it, in its own
words or by pointing to the type that states them.

- **Threads.** Every public member may be called from any thread at any time, except where
  it says otherwise: a `WorkTransaction` is used by one caller at a time, as its
  `DbTransaction` is, and `WorkWorker.RunAsync` runs once.
- **Errors.** An argument error is thrown by the call, before anything happens:
  `ArgumentNullException` for a `null` argument that is never allowed,
  `ArgumentOutOfRangeException` for a number or a `TimeSpan` outside its range,
  `ArgumentException` for any other wrong value, a `null` that another argument's rule
  forbids and a required option left `null` included; `ParamName` is the parameter's name,
  or the property's name for an option. `InvalidOperationException` for a misuse is thrown
  by the call. `NotSupportedException` is thrown by the call, after the argument checks,
  until the member's sprint. A failure of the operation — the driver's `DbException`,
  `OperationCanceledException` — is reported by the returned task.
- **Cancellation.** A token that is already cancelled, once the argument checks passed,
  gives a cancelled task before anything else, `NotSupportedException` included, and
  nothing is written. A task whose token is cancelled later ends cancelled, except
  `WorkWorker.RunAsync`, for which the token is the stop: its task then completes.
- **Results.** Expected outcomes are results, not exceptions. The value 0 of a result enum
  is never returned, so a `default` result claims nothing.
- **Options** are plain classes with settable properties and defaults. The constructor
  that takes them validates and copies them; a later change of the options object has no
  effect. A `TimeSpan` of `WorkQueueOptions` is greater than zero and at most 3650 days; a
  `TimeSpan` of `WorkWorkerOptions` is greater than zero and at most
  `TimeSpan.FromMilliseconds(uint.MaxValue - 1)`, the longest timer of `TimeProvider`.
- **Names.** A queue name is 1 to 100 characters, each one of `A–Z a–z 0–9 _ -`. A key or a
  scope is 1 to 200 characters (`string.Length`), contains no control character
  (`char.IsControl`) and no unpaired surrogate, and does not end with a space (W15). A
  payload is any string without U+0000 or an unpaired surrogate (W4). All of them compare
  exactly, case included.
- **Time.** Due times and lease ends are the database's (rule 12, W5–W7). A worker's own
  timers use the `TimeProvider` of its options.

### S1 · The core contract and the test lane

**Track:** full · **Branch:** `epic-1/sprint-01-core-contract` · **Implementer:** Opus 5.5,
`high` · **Needs:** the owner's approval of this plan.

- **Goal.** The package `Moltaro.Work` exists with the whole contract of an application and
  of a provider, documented; its checks work; `dotnet test` runs the first tests.
- **Projects.**
  - `Moltaro.Work`: packed, with a package description and `GenerateDocumentationFile`;
    references the package `Moltaro.Signals` 0.1.0 and nothing else;
    `InternalsVisibleTo Moltaro.Work.Tests`. Every public type is in the namespace
    `Moltaro.Work`, in the project's root folder: the contract of the application and the
    provider contract use each other's types, so one namespace keeps the dependencies of
    folders in one direction (principle 12). Internal folders come with later sprints;
    the decision component of S7 depends on nothing of the library, and the root uses it.
  - `Moltaro.Work.Tests`: not packed; references `Moltaro.Work`.
  - `Moltaro.Work.Samples`: not packed; a console program; references `Moltaro.Work`.
  - `src/global.json` gains `"test": { "runner": "Microsoft.Testing.Platform" }`;
    `Directory.Packages.props` gains `Moltaro.Signals` 0.1.0 and `xunit.v3`; all three
    projects go into `Moltaro.Work.slnx`.
- **Contract, namespace `Moltaro.Work`: the application's part.**

  ```csharp
  public enum WorkShapeEnum { Coalescing = 1, Log = 2, Run = 3 }

  public sealed class WorkQueueOptions
  {
      public int AttemptLimit { get; set; } = 10;                              // 1 to 1000
      public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);
      public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);    // at least RetryDelay
      public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
      public bool Scoped { get; set; }
  }

  public sealed class WorkQueue
  {
      public WorkQueue(string name, WorkShapeEnum shape, WorkQueueOptions? options = null);
      public string Name { get; }
      public WorkShapeEnum Shape { get; }
      public int AttemptLimit { get; }
      public TimeSpan RetryDelay { get; }
      public TimeSpan MaxRetryDelay { get; }
      public TimeSpan Retention { get; }
      public bool Scoped { get; }
  }

  public sealed class WorkClient
  {
      public WorkClient(IWorkStore store, ISignals signals);
      public WorkTransaction Join(DbTransaction transaction);
      public Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken = default);
  }

  public sealed class WorkTransaction
  {
      public Task<WorkEnqueueResult> EnqueueAsync(WorkQueue queue, string? key = null, string? scope = null,
          string? payload = null, DateTimeOffset? dueAt = null, CancellationToken cancellationToken = default);
      public Task CommitAsync(CancellationToken cancellationToken = default);
  }

  public enum WorkEnqueueStatusEnum { Added = 1, Coalesced = 2 }
  public readonly record struct WorkEnqueueResult(WorkEnqueueStatusEnum Status, long ItemId);
  public enum WorkRequeueStatusEnum { Requeued = 1, NotDead = 2, KeyActive = 3 }

  public interface IWorkHandler
  {
      Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken);
  }
  public enum WorkHandlerResultEnum { Completed = 1, Retry = 2 }

  public sealed class WorkItem
  {
      public WorkItem(WorkQueue queue, long id, string? key, string? scope, string? payload, int attempt);
      public WorkQueue Queue { get; }
      public long Id { get; }
      public string? Key { get; }
      public string? Scope { get; }
      public string? Payload { get; }
      public int Attempt { get; }
  }

  public sealed class WorkWorkerOptions
  {
      public int Lanes { get; set; } = 1;                                        // 1 to 1000
      public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
      public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10); // at most half of LeaseDuration
      public TimeSpan IdleBound { get; set; } = TimeSpan.FromMinutes(1);
      public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(5);
      public int RetentionBatchSize { get; set; } = 1000;                       // 1 to 100000
      public TimeProvider TimeProvider { get; set; } = TimeProvider.System;      // not null
  }

  public sealed class WorkWorker
  {
      public WorkWorker(WorkClient client, WorkQueue queue, IWorkHandler handler, WorkWorkerOptions? options = null);
      public WorkQueue Queue { get; }
      public event EventHandler<WorkWorkerFailedEventArgs>? Failed;
      public Task RunAsync(CancellationToken stoppingToken);
  }

  public sealed class WorkWorkerFailedEventArgs : EventArgs
  {
      internal WorkWorkerFailedEventArgs(Exception exception);
      public Exception Exception { get; }
  }
  ```

- **Contract, namespace `Moltaro.Work`: the provider contract.**

  ```csharp
  public interface IWorkStore
  {
      Task CheckDatabaseAsync(CancellationToken cancellationToken);
      Task<WorkEnqueueResult> EnqueueAsync(DbTransaction transaction, WorkQueue queue, string? key, string? scope,
          string? payload, DateTimeOffset? dueAt, CancellationToken cancellationToken);
      Task<IReadOnlyList<WorkClaim>> ClaimAsync(WorkQueue queue, int count, TimeSpan lease, CancellationToken cancellationToken);
      Task<IReadOnlyList<WorkClaim>> HeartbeatAsync(IReadOnlyList<WorkClaim> claims, TimeSpan lease, CancellationToken cancellationToken);
      Task<bool> CompleteAsync(WorkClaim claim, CancellationToken cancellationToken);
      Task<bool> RetryAsync(WorkClaim claim, TimeSpan delay, string reason, CancellationToken cancellationToken);
      Task<bool> GiveUpAsync(WorkClaim claim, string reason, CancellationToken cancellationToken);
      Task<bool> ReleaseAsync(WorkClaim claim, CancellationToken cancellationToken);
      Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken);
      Task<WorkNextDue> GetNextDueAsync(WorkQueue queue, CancellationToken cancellationToken);
      Task<int> DeleteExpiredAsync(WorkQueue queue, int count, CancellationToken cancellationToken);
  }

  public sealed class WorkClaim
  {
      public WorkClaim(WorkItem item, long fence);
      public WorkItem Item { get; }
      public long Fence { get; }
  }
  public readonly record struct WorkNextDue(TimeSpan? UntilClaimable, TimeSpan? UntilExpired);
  ```

- **What each member checks, does and documents.**
  - `WorkShapeEnum`: each value documents its row of the concept's table, with W2–W4 and
    the answers to PQ1 and PQ2: what one item is per, what an enqueue does while one waits
    and while one is held, and what becomes of it after success.
  - `WorkQueue(...)`: `name` `null` → `ArgumentNullException`; breaks the rule →
    `ArgumentException`; `shape` undefined → `ArgumentOutOfRangeException`; `options`
    `null` means the defaults; each option out of range → `ArgumentOutOfRangeException`,
    `MaxRetryDelay` below `RetryDelay` → `ArgumentException` (`ParamName`
    `MaxRetryDelay`). The type documents: a queue is a declaration, with no database
    behind it; a name means one queue, which every process declares with the same shape
    and options, and the library cannot check that across processes; its signal key and
    group (W11); what each property means: `AttemptLimit` the most attempts before the item
    is dead, `RetryDelay` the delay after the first failed attempt, doubling after each
    one up to `MaxRetryDelay`, `Retention` how long dead items and finished runs are kept,
    `Scoped` whether items may carry a scope (rule 10).
  - `WorkClient(...)`: `null` arguments → `ArgumentNullException`. `Join` returns a new
    `WorkTransaction` at once; `null` → `ArgumentNullException`; it neither reads nor
    changes the transaction. `RequeueAsync`: `queue` `null` → `ArgumentNullException`,
    `itemId` below 1 → `ArgumentOutOfRangeException`; then `NotSupportedException` until
    S9; documents W8 and each status.
  - `WorkTransaction.EnqueueAsync`: checks in this order, each before the store is called:
    `queue` `null` → `ArgumentNullException`; after `CommitAsync` was called, or after an
    enqueue of this transaction failed, or when `transaction.Connection` is `null` because
    the transaction ended → `InvalidOperationException`; for a coalescing queue `key` `null`
    → `ArgumentException` (`key`) and a `payload` → `ArgumentException` (`payload`); a `key`
    or `scope` that breaks the rule → `ArgumentException`; a `scope` on a queue that is not
    `Scoped` → `ArgumentException` (`scope`); a `payload` that breaks its rule →
    `ArgumentException`; a token already cancelled → a cancelled task. Then it calls
    `IWorkStore.EnqueueAsync` with the arguments unchanged, returns its result and remembers
    the queue for `CommitAsync`. Documents: the item exists only if the transaction commits
    (rule 1); `dueAt` as W5; the result's statuses; the errors of W10, and that the caller
    then rolls back and retries: once an enqueue's task failed, the database may have undone
    that statement alone, so the transaction takes no more enqueues and no `CommitAsync`,
    which would keep the write without its item.
  - `WorkTransaction.CommitAsync`: a second call, the first counting whatever its outcome,
    or a call after a failed enqueue → `InvalidOperationException`; then
    `NotSupportedException` until S5. Documents: it commits the transaction; only after the
    commit succeeded it sets the signal of every queue enqueued into, once each (rule 2,
    W11); an exception of the commit propagates and nothing is set; once committed it
    reports nothing about the sets, because a set that is lost or fails is covered by
    `Joined` after a reconnect and by the idle bound (rule 3); a rollback is the caller's,
    on its `DbTransaction`.
  - `WorkEnqueueResult`, `WorkEnqueueStatusEnum`: `Added` — an item became waiting that was
    not waiting or held before: a new item, or a dead coalescing item revived (PQ1);
    `Coalesced` — an item already waiting or held absorbed the enqueue (W2, W3).
    `ItemId` is the id of the added item or of the one that absorbed it.
  - `WorkRequeueStatusEnum`: `Requeued`; `NotDead` — no dead item with that id in that
    queue; `KeyActive` — the dead log or run item's key belongs to an active item (W8).
  - `IWorkHandler`: documents rule 9 (it may run more than once for an item and is
    written to be idempotent); that an exception is a failure and `Retry` asks for one
    (W13); that a returned value that is not defined counts as a failure; that the token
    is cancelled when the worker stops (W9) or when the lease is lost, and that the item's
    outcome is written by the worker after the handler returns.
  - `WorkItem(...)`: `queue` `null` → `ArgumentNullException`; `id` or `attempt` below 1 →
    `ArgumentOutOfRangeException`; nothing else is checked, because a provider creates it
    from a row. `Attempt` is 1 for the first claim and counts every claim, a lost lease
    included (rule 6).
  - `WorkWorkerOptions`: each property documents its default and its range; `Lanes` is the
    number of handlers that run at once and the size of every claim (W14); `LeaseDuration`
    the lease each claim and heartbeat sets; `HeartbeatInterval` above half of
    `LeaseDuration` → `ArgumentException` (`ParamName` `HeartbeatInterval`), so that one
    late heartbeat does not lose the lease; `IdleBound` as rule 3; `FailureDelay` as W12;
    `RetentionBatchSize` the most items one delete removes; `TimeProvider` `null` →
    `ArgumentException`, and it has to be the clock of the `ISignals` of the client (W6).
  - `WorkWorker(...)`: `null` `client`, `queue` or `handler` → `ArgumentNullException`;
    options as above. `RunAsync`: a second call → `InvalidOperationException`; then
    `NotSupportedException` until S7. Documents: it first calls
    `IWorkStore.CheckDatabaseAsync`, and its `InvalidOperationException` ends the run; it
    subscribes as W11, claims, runs the handlers on its lanes, heartbeats, writes each
    outcome, waits as rule 3, W6, W7 and W16 say; its task completes when `stoppingToken` is
    cancelled and every handler has ended, each outcome written once and a failed write
    leaving its item to its lease (W9); it never ends on a database error (W12), but a
    `WaitAsync` that fails other than by the stop's cancellation ends it with that
    exception, after the stop, because the worker could then wait for nothing. `Failed` is
    raised on the worker's loop, never under a lock, once for every statement of the worker
    that failed, before the worker waits `FailureDelay`; an exception thrown by a subscriber
    ends `RunAsync` with that exception, after the stop.
  - `IWorkStore`: documents that it is the provider contract, implemented once per
    database; that every method may be called concurrently; that the core calls it only
    with arguments it has checked, so an implementation checks only what the core cannot,
    such as the type of the transaction's connection (`ArgumentException`);
    `EnqueueAsync` runs on `transaction.Connection` inside `transaction`, and every other
    method on a connection and a transaction of its own at the isolation the provider
    names; each method's effect, in the words of `docs/storage.md`; that a fenced write
    returns `false` when the claim's fence is not the item's current one, and writes
    nothing then; that `HeartbeatAsync` returns the claims it refused; `ReleaseAsync` as
    PQ7 (b); that
    `CheckDatabaseAsync` throws `InvalidOperationException` naming the schema version it
    found and the one it needs, or the database setting that is off.
  - `WorkClaim(...)`: `item` `null` → `ArgumentNullException`; `fence` below 1 →
    `ArgumentOutOfRangeException`. `WorkNextDue`: each value is a duration computed by the
    database from its now, zero when already reached, `null` when there is none (W6, W7).
- **To read.** [Concept](../concept.md) §2–§5 and §9; the public contract of
  Moltaro.Signals 0.1.0 at its tag `v0.1.0`: `ISignals`, `ISignalSubscription`, `SignalHost`.
- **Acceptance.** Tests that pass, a folder per area:
  - `Queues`: the name rule at its edges (empty, 100 and 101 characters, `.`, a space, a
    letter outside ASCII); an undefined shape; every option at and beyond each end of its
    range with its `ParamName`; `MaxRetryDelay` below `RetryDelay`; `null` options give the
    defaults; changing the options after the constructor changes nothing.
  - `Enqueue`: every check of `EnqueueAsync` in its order, and that the store is not called
    when one fails; that valid arguments reach the store unchanged; `Join(null)`. The test
    project has a `DbTransaction` and a `DbConnection` of its own that touch no database,
    and an `IWorkStore`
    that records its calls and throws for everything else; the client's `ISignals` is an
    in-process `SignalHost`.
  - `Workers`: the checks of `WorkWorker`, `WorkWorkerOptions`, `WorkItem` and `WorkClaim`,
    with `HeartbeatInterval` at and above half of `LeaseDuration`.
  - `Dependencies`: `Moltaro.Work` references only assemblies of .NET and `Moltaro.Signals`
    (principle 12).

  No test is skipped in S1: what is not implemented has its tests in the sprint that
  needs a database. The sample `DeclaringQueues` declares one queue of each shape and a
  handler, prints their settings, shows the `ArgumentException` of a wrong name, and
  stops, saying that enqueueing needs the PostgreSQL provider of S2. The documents step
  corrects every sentence of `AGENTS.md`, `README.md`, `docs/README.md` and the work README
  that says that no code exists or that the solution is empty, and makes the edits of the
  owner's answers (Rules for every sprint, "Documents").
- **Out of scope.** Providers, hosting, every behavior beyond the checks, the decision
  component, `docs/storage.md`.

### S2 · The PostgreSQL provider contract and schema

**Track:** full · **Branch:** `epic-1/sprint-02-postgresql-schema` · **Implementer:** Opus
5.5, `xhigh` · **Needs:** S1.

- **Goal.** The package `Moltaro.Work.PostgreSql` exists with its store, refusing, and the
  schema script of version 1, which applies to a real PostgreSQL in CI;
  `docs/storage.md` says what the provider contract does and what the schema is; the
  conformance suite exists as a frame whose cases are skipped.
- **Projects.**
  - `Moltaro.Work.PostgreSql`: packed, with a description and `GenerateDocumentationFile`;
    references `Moltaro.Work` and `Npgsql`; the script is the embedded resource
    `Schema/version-1.sql`.
  - `Moltaro.Work.TestSupport`: not packed and no test project; references `Moltaro.Work`
    and `xunit.v3.extensibility.core` for the attributes, not a runner. It holds the
    shared skip reason; folder `Conformance` holds the suite, as PQ6 (a).
  - `Moltaro.Work.PostgreSql.Tests`: not packed; references the provider and TestSupport.
  - CI: the `build` job and the job of Publish gain the service container `postgres:15`
    with a health check and `MOLTARO_WORK_POSTGRESQL`, so that the tag's tests pass too; the
    `windows` job runs `Moltaro.Work.Tests` only. S2 records X1 as PQ5 says. The samples
    gain a reference to the provider.
- **Contract, namespace `Moltaro.Work.PostgreSql`.**

  ```csharp
  public sealed class PostgreSqlWorkStore : IWorkStore
  {
      public const int SchemaVersion = 1;
      public PostgreSqlWorkStore(DbDataSource dataSource);
      public static string SchemaScript { get; }
      // Every member of IWorkStore, documented as the provider contract says.
  }
  ```

  - The constructor: `null` → `ArgumentNullException`; a data source whose connections
    are not `NpgsqlConnection` → `ArgumentException` (it creates one connection to see,
    opens nothing, and disposes it). It keeps the data source, which the application owns
    and disposes.
  - `SchemaScript` returns the text of version 1, as below, from the first sprint on.
  - Each member of `IWorkStore` throws `NotSupportedException` until its sprint:
    `EnqueueAsync` S5 for a coalescing queue, S10 for a log or run queue, S11 with a scope;
    `ClaimAsync` S6, S11 for a scoped queue; `HeartbeatAsync` S6; `CompleteAsync` S6 for a
    coalescing item, S10 for a log or run item; `CheckDatabaseAsync` and `GetNextDueAsync`
    S7; `RetryAsync`, `GiveUpAsync`, `ReleaseAsync` and `RequeueAsync` S9;
    `DeleteExpiredAsync` S12.
  - The type documents: PostgreSQL 15 or later, the oldest release supported on 26 November
    2026, with a database encoding of UTF8, which `CheckDatabaseAsync` checks with the
    schema version; its own transactions run at READ COMMITTED, which it sets on each; the
    caller's transaction may run at any isolation, and at REPEATABLE READ or SERIALIZABLE an
    enqueue that meets a concurrent change of its key fails with `serialization_failure`
    (40001), and any enqueue may fail with `deadlock_detected` (40P01), both as
    `PostgresException` (W10).
- **The schema, version 1.** The text of `Schema/version-1.sql`, which the implementer
  writes as it stands here, apart from comments:

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

  What the schema means, which `docs/storage.md` says once for all three databases:
  - `state`: 1 waiting, 2 held, 3 finished, 4 dead. A coalescing item is never finished:
    it is deleted.
  - `active_key` reserves a key: for a coalescing item it is `item_key` in every state
    (one row per key, W3); for a log or run item it is `item_key` while waiting or held and
    `NULL` otherwise (W2). Its unique index is the one mechanism of "one per key" in every
    provider.
  - `due_at` is when a waiting item may be claimed; `lease_until` when a held item's lease
    ends; `again_due_at` the mark "run again" of W3; `fence` grows by one with every claim
    and is never reset (W1); `attempts` counts claims since the item was added, revived or
    requeued; `enqueued_at`, `claimed_at` (the last claim) and `finished_at` (finished or
    dead) are the times of a run's history; `reason` the last failure, at most 4000
    characters.
  - A row of `moltaro_work_scope` exists exactly while an item of that scope is held, and
    names it; its primary key is the scope rule without advisory locks (D9, S11).
  - The schema's version is the one row of `moltaro_work_schema`; a later version comes
    with a script that changes the tables and that row.
  - No foreign key, because `READPAST` and skip-locked reads still wait on them.
- **`docs/storage.md`.** Written by this sprint: what each operation of `IWorkStore` does
  to an item, per state and shape, as this table says; the meaning of the schema above;
  for PostgreSQL its script, minimum version, isolation and the errors of an enqueue. It
  carries the schema version. PQ4 (a) makes it the contract an application reads runs by.

  | Operation | Applies to | Effect, all times by the database's clock |
  | --- | --- | --- |
  | Enqueue | No row with that active key | A waiting row; `due_at` the later of the due time and now (W5) |
  | | Coalescing, waiting | `due_at` the earlier of the two, unless it waits for a retry (PQ8) |
  | | Coalescing, held | `again_due_at` the earlier of the two |
  | | Coalescing, dead | Waiting, `due_at` as a new one, `attempts` 0 (PQ1) |
  | | Log or run, active key | Nothing; `Coalesced` |
  | Claim | Waiting with `due_at` reached, or held with `lease_until` reached | Held; `fence` and `attempts` plus one; `lease_until` now plus the lease; `claimed_at` now; at most one item per scope, with its scope row |
  | Heartbeat | Held, with the claim's fence | `lease_until` now plus the lease |
  | Complete | Held, with the claim's fence | Coalescing: deleted, or waiting at `again_due_at` with `attempts` 0; log: deleted; run: finished |
  | Retry | Held, with the claim's fence | Waiting at now plus the delay, with the reason |
  | Give up | Held, with the claim's fence | Dead with the reason; a coalescing item with a mark as PQ1 says |
  | Release | Held, with the claim's fence | Waiting at now, with the attempts it had before the claim (PQ7) |
  | Requeue | Dead | Waiting at now, `attempts` 0 (W8) |
  | Delete expired | Finished or dead, `finished_at` older than the retention | Deleted, at most the batch, oldest first |

  A fenced write that ends a hold also deletes the item's scope row. A finished or dead
  log or run item gives up its `active_key`.
- **To read.** [The study](#the-study) and this plan's W1–W16; the PostgreSQL documentation
  of `INSERT … ON CONFLICT`, partial indexes, `SELECT … FOR UPDATE SKIP LOCKED` and its
  date and time functions.
- **Acceptance.** Tests that pass, on PostgreSQL in CI:
  - the script applies to an empty database, and `moltaro_work_schema` then holds 1, which
    is `SchemaVersion`;
  - the constructor's checks, with a data source of the test project that is not Npgsql's;
  - `Moltaro.Work.PostgreSql` references only assemblies of .NET, `Moltaro.Work` and
    Npgsql.

  The conformance suite's frame: an abstract `ConformanceDatabase` in TestSupport that a
  provider's test project derives, with `CreateStore()`, `OpenConnectionAsync(…)` and
  `MoveClockAsync(WorkQueue queue, TimeSpan by)`, which shifts every time of the queue's
  rows back by `by`; and one abstract test class per area — `Enqueue`, `Claims`, `Worker`,
  `Retries`, `Shapes`, `Scopes`, `Retention` — each derived by
  `Moltaro.Work.PostgreSql.Tests`. Each area holds, skipped, the cases of
  [Rules and their tests](#rules-and-their-tests) that name its sprint; later sprints add
  the cases of their briefs. The sample `EnqueueInTransaction` applies `SchemaScript` where
  it is missing, makes a business write and a coalescing enqueue in one transaction,
  commits with `CommitAsync`, and stops where `EnqueueAsync` refuses.
- **Out of scope.** Any SQL besides the script and the test project's clock statement; the
  other providers.

### S3 · The SQL Server and MySQL provider contracts and schemas

**Track:** full · **Branch:** `epic-1/sprint-03-sqlserver-mysql-schemas` · **Implementer:**
Opus 5.5, `xhigh` · **Needs:** S2.

- **Goal.** The packages `Moltaro.Work.SqlServer` and `Moltaro.Work.MySql` exist, refusing,
  with schema scripts of version 1 that mean what S2's does and apply to the real databases
  in CI; `docs/storage.md` covers them.
- **Projects.** Both packed, as S2's, referencing `Moltaro.Work` and
  `Microsoft.Data.SqlClient` or `MySqlConnector`; `Moltaro.Work.SqlServer.Tests` and
  `Moltaro.Work.MySql.Tests` as S2's. The `build` job and the job of Publish gain the
  service containers `mcr.microsoft.com/mssql/server:2019-latest` and `mysql:8.4`, with
  their variables; the SQL Server test project turns READ_COMMITTED_SNAPSHOT on in the
  database it creates.
- **Contract.**

  ```csharp
  namespace Moltaro.Work.SqlServer;

  public sealed class SqlServerWorkStore : IWorkStore
  {
      public const int SchemaVersion = 1;
      public SqlServerWorkStore(DbDataSource dataSource);   // SqlClientFactory.Instance.CreateDataSource(…)
      public static string SchemaScript { get; }
  }
  ```

  ```csharp
  namespace Moltaro.Work.MySql;

  public sealed class MySqlWorkStore : IWorkStore
  {
      public const int SchemaVersion = 1;
      public MySqlWorkStore(DbDataSource dataSource);       // a MySqlDataSource
      public static string SchemaScript { get; }
  }
  ```

  Constructors and `SchemaScript` as S2's, with `SqlConnection` and `MySqlConnection`.
  Every member of `IWorkStore` throws `NotSupportedException` until S14 or S15.
  - `SqlServerWorkStore` documents: SQL Server 2019 or later, or Azure SQL Database; the
    database needs READ_COMMITTED_SNAPSHOT ON, which `CheckDatabaseAsync` checks before a
    worker runs, because without it candidate reads deadlock with claims; its claims read
    with `UPDLOCK, READPAST`, because under that setting a plain read does not skip locked
    rows; its own transactions run at READ COMMITTED; at the caller's isolation an enqueue
    may fail as a deadlock victim (1205), and under SNAPSHOT with an update conflict (3960),
    both as `SqlException` (W10); every session that changes the tables, a caller's
    included, needs the SET options of filtered indexes — ANSI_NULLS, ANSI_PADDING,
    ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL and QUOTED_IDENTIFIER ON,
    NUMERIC_ROUNDABORT OFF — which SqlClient's defaults meet at a compatibility level of
    90 or higher; the script sets them itself, because `sqlcmd` leaves QUOTED_IDENTIFIER
    OFF; lock escalation is off for the item table, so that a large claim or delete never
    locks every queue.
  - `MySqlWorkStore` documents: MySQL 8.4 or later with InnoDB and row-based binary
    logging, because skip-locked reads are unsafe for statement-based replication; MariaDB
    is not supported; its own transactions run at READ COMMITTED, which it sets on each; at
    MySQL's default REPEATABLE READ, concurrent enqueues of one key in callers'
    transactions can deadlock (1213) or wait out `innodb_lock_wait_timeout` (1205), as
    `MySqlException` (W10).
- **The schemas, version 1.** `Schema/version-1.sql` of each, written as it stands here.

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

  The differences: SQL Server's filtered indexes stand for PostgreSQL's partial ones, and
  its checks are spelled out. MySQL has no partial index: a unique index over a nullable
  `active_key` admits any number of `NULL`, which gives the same "one per active key"; its
  waiting and held indexes lead with `state`, and the ended one needs none, because the
  checks set `finished_at` exactly for ended rows. `datetime2(6)` and `datetime(6)` keep
  microseconds; the binary collations keep W15, and `utf8mb4_0900_bin` does not pad.
- **To read.** S2's brief and `docs/storage.md` as S2 left it.
- **Acceptance.** For each of the two, as S2's: the script applies and records 1, the
  constructor's checks, the references (`Microsoft.Data.SqlClient` or `MySqlConnector`),
  and the suite's areas derived and skipped. Every test of the three provider projects runs
  in the `build` job.
- **Out of scope.** Any SQL besides the scripts and the test projects' clock statements.

### S4 · The hosting contract and the samples

**Track:** full · **Branch:** `epic-1/sprint-04-hosting-contract` · **Implementer:** Opus
5.5, `high` · **Needs:** S3.

- **Goal.** The package `Moltaro.Work.Hosting` exists, refusing after its checks; the
  samples show the whole intended use and report where they stop; `dotnet pack` produces the
  five packages.
- **Projects.** `Moltaro.Work.Hosting`: packed, as the others; references `Moltaro.Work` and
  `Microsoft.Extensions.Hosting.Abstractions`. `Moltaro.Work.Hosting.Tests`: not packed;
  references it, TestSupport, `Microsoft.Extensions.Hosting` and
  `Moltaro.Signals.Hosting`. The samples gain references to the hosting package, the
  PostgreSQL provider, `Moltaro.Signals.Hosting` and `Microsoft.Extensions.Hosting`. The
  `windows` job also runs `Moltaro.Work.Hosting.Tests`.
- **Contract, namespace `Moltaro.Work.Hosting`.**

  ```csharp
  public static class WorkServiceCollectionExtensions
  {
      public static IServiceCollection AddWork(this IServiceCollection services,
          Func<IServiceProvider, IWorkStore> createStore);

      public static IServiceCollection AddWorkWorker<THandler>(this IServiceCollection services,
          WorkQueue queue, Action<WorkWorkerOptions>? configure = null)
          where THandler : class, IWorkHandler;
  }
  ```

  - Both: a `null` argument that is not optional → `ArgumentNullException`; then
    `NotSupportedException` until S8.
  - `AddWork` documents: it registers the `IWorkStore` that `createStore` makes, once, and
    a `WorkClient` over it and the container's `ISignals`, both singletons; the `ISignals`
    comes from `AddSignalHost` or `AddSignalClient` of `Moltaro.Signals.Hosting`, and a
    container without one fails when the client is resolved; the application owns and
    disposes what the store uses, such as its data source.
  - `AddWorkWorker<THandler>` documents: it registers `THandler` as scoped, unless it is
    registered already, and a hosted service that creates a `WorkWorker` of `queue` with
    the options `configure` fills, when it is resolved; wrong options fail the start of the
    application. For every item it creates a scope of its own and resolves `THandler` in
    it. The worker runs from the start of the application until it stops, bounded by the
    host's shutdown timeout (W9). A second worker of a queue with the same name in one
    container → `ArgumentException` (`queue`), from S8. It writes these log lines through
    the container's `ILoggerFactory`, with the named values as structured properties and a
    message text that is not part of the contract: category `Moltaro.Work.WorkWorker`,
    Warning, when a handler threw, with `Queue`, `ItemId`, `Attempt` and the exception;
    the same category, Error, when the worker raised `Failed`, with `Queue` and the
    exception.
- **To read.** S1's brief; `SignalServiceCollectionExtensions` of `Moltaro.Signals.Hosting`
  0.1.0.
- **Acceptance.** Tests that pass: the argument checks of both methods; the references of
  `Moltaro.Work.Hosting` (assemblies of .NET, `Microsoft.Extensions.*`, `Moltaro.Work`,
  `Moltaro.Signals`); `dotnet pack -c Release` from `src` produces exactly the five
  packages. Skipped, folder `Registration`: the client resolves over the registered store
  and `ISignals`; a worker's handler runs in a scope of its own per item; a worker stops
  with the application after its handlers ended; a second worker of one queue is refused;
  the two log lines. The sample `GenericHostWorker`: a generic host with `AddSignalHost`
  (in-process), `AddWork` over `PostgreSqlWorkStore` and `AddWorkWorker` of a coalescing
  queue, an enqueue in a transaction after the start, and a handler that stops the
  application when it ran; it stops where `AddWork` refuses. The samples `WorkerWithoutHost`
  (a worker over an in-process `SignalHost`) and `LogAndRun` (a log item with a payload, a
  run with a key, a scoped item, a handler that returns `Retry`, a requeue) are written too,
  and stop where they refuse.
- **Out of scope.** Any behavior beyond the checks; overloads that bind options from
  configuration; health checks.

## Stage 2: the thinnest path on PostgreSQL

Its contract is written before S5, as the rules for every sprint say; the public contract
of Stage 1 is expected to carry it unchanged.

### S5 · Coalescing enqueue and the commit that wakes, on PostgreSQL

**Track:** full · **Branch:** `epic-1/sprint-05-coalescing-enqueue` · **Implementer:** Opus
5.5, `xhigh` · **Needs:** S4 and the approval of Stage 2's contract.

- **Goal.** A coalescing item enqueued in the caller's transaction on PostgreSQL exists only
  if that transaction commits, and `CommitAsync` then sets its queue's signal.
- **Contract.** `PostgreSqlWorkStore.EnqueueAsync` for an unscoped coalescing queue, as
  `docs/storage.md`, W3 and W10 say; `WorkTransaction.CommitAsync`.
- **Acceptance.** Conformance, area `Enqueue`: a second enqueue of a committed key reports
  `Coalesced` with the first's id, and after a rollback `Added`; two transactions that
  enqueue one key at once at READ COMMITTED leave one item; at REPEATABLE READ the second
  fails as W10 says. The queue's member is signalled after `CommitAsync`, not after a
  rollback, a failed commit or a failed enqueue. The sample `EnqueueInTransaction` runs to
  the end.
- **Out of scope.** Claims, logs, runs, scopes.

### S6 · Claim, lease, heartbeat and fenced completion

**Track:** full · **Branch:** `epic-1/sprint-06-claim-lease-fence` · **Implementer:** Opus
5.5, `xhigh` · **Needs:** S5.

- **Goal.** On PostgreSQL a coalescing item is claimed in a batch with a new fence, kept by
  heartbeats, claimed again by the next claim after its lease ended, with no rescuer
  process, and completed only by its current holder.
- **Contract.** `ClaimAsync` for an unscoped queue, `HeartbeatAsync`, and `CompleteAsync`
  for a coalescing item.
- **Acceptance.** Conformance, area `Claims`: many claimers at once never return one item
  twice and together return all; a claim returns at most `count`; an item due later by the
  database's clock is claimed only once that clock is moved; an enqueue while waiting moves
  the due time earlier, unless the item waits for a retry (PQ8), and one while held makes
  the item run once more after its completion; an item whose holder died is claimed by
  another with the next attempt and a new fence, unless a heartbeat kept its lease; a
  holder whose item was claimed again has its heartbeat and its completion refused; a
  completion while an enqueue of the key is uncommitted waits for it, then makes the item
  waiting again with attempts from zero if it committed and deletes it if it rolled back,
  and is never refused with a current fence; a transaction that keeps one held item locked
  delays no heartbeat of the other claims. Statements per item for claim and completion
  are reported.
- **Out of scope.** The worker loop, retries, scopes.

### S7 · The worker: lanes, waking and stop

**Track:** full · **Branch:** `epic-1/sprint-07-worker` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S6.

- **Goal.** The thinnest path end to end: a committed coalescing item is handled by a
  `WorkWorker` woken through Moltaro.Signals; due times wake it on the database's clock
  (W6); the idle bound limits a lost signal.
- **Contract.** `WorkWorker.RunAsync`; `CheckDatabaseAsync` and `GetNextDueAsync` of
  PostgreSQL; the decision component, folder and namespace `Moltaro.Work.Policy`, depending
  on nothing of the library, for the wait of W6 and for when a lease counts as lost, its
  internal types named with the stage's contract. No lock is held across the await of a
  handler, and each claim's lease is judged by its own heartbeats. Until S9 a failed
  handler's item is left to its lease, and the refusal of `RetryAsync` is reported through
  `Failed`. If one agent and one review cannot carry S7, the stage's contract splits it:
  the decision component with the waking, then the lanes with heartbeats and stop.
- **Acceptance.** Policy tests of the wait and of lease loss at their edges. Conformance,
  area `Worker`, with an in-process `SignalHost` and one `FakeTimeProvider` for host and
  worker: a commit starts the handler without moving a clock; an item due in 30 seconds
  starts once both clocks moved; an idle worker sends no statement before its idle bound
  and one then, counted through the driver's tracing; two workers of one lane each share a
  commit of three items without waiting for an idle bound (W16); a due item locked by an
  open transaction costs no statement until that transaction ends or the idle bound (W7);
  with three lanes at most three handlers run at once; a handler that outlives the lease
  keeps its item; one whose item was claimed again, by another worker or on another lane
  of its own, has its token cancelled and its completion refused; a stop cancels the
  handlers and `RunAsync` completes after they ended; a wrong schema version ends
  `RunAsync` with `InvalidOperationException`; an unreachable database raises `Failed` and
  the worker goes on after `FailureDelay`; a failing `WaitAsync` ends `RunAsync`. The sample
  `WorkerWithoutHost` runs to the end.
- **Out of scope.** Retries, hosting.

### S8 · The hosting package works

**Track:** full · **Branch:** `epic-1/sprint-08-hosting` · **Implementer:** Opus 5.5,
`high` · **Needs:** S7.

- **Goal.** `AddWork` and `AddWorkWorker` work as S4 documents them, the check of a second
  worker included; the sample `GenericHostWorker` runs to the end.
- **Acceptance.** S4's tests of `Registration` pass, the log lines read through a logger
  provider of the test. The hosting tests use PostgreSQL from here on, so the `windows` job
  runs `Moltaro.Work.Tests` only.
- **Out of scope.** What the worker does beyond S7.

## Stage 3: hardening and widening on PostgreSQL

Its contract, with the metrics of S13, is written before S9.

### S9 · Retries, dead items, release and requeue

**Track:** full · **Branch:** `epic-1/sprint-09-retries` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S8 and the approval of Stage 3's contract.

- **Goal.** A failed or retried attempt is due again after its backoff; after the attempt
  limit the item is dead with its reason; a requeue puts it back; a stop releases what runs.
- **Contract.** `RetryAsync`, `GiveUpAsync`, `ReleaseAsync` and `RequeueAsync` of
  PostgreSQL; `WorkClient.RequeueAsync`; PQ1 in the enqueue. The decision component decides
  an attempt: after a claim, run or give up; after it ended — completed, retry asked,
  failed, stopped — complete, retry after which delay, give up or release (W9, W13); the
  delay of attempt *n* is `RetryDelay` doubled *n − 1* times, at most `MaxRetryDelay`,
  without overflow.
- **Acceptance.** Policy tests of each rule at its edges. Conformance, area `Retries`: a
  failing handler's item is due again after the backoff by the database's clock, with the
  next attempt; after `AttemptLimit` failures it is dead with the exception's text; a lease
  lost on the last attempt makes it dead at the next claim, and the handler does not run;
  a stop releases a running item as PQ7 says and wakes another worker (W16); a requeue
  makes a dead item waiting with attempts from zero, and of another item reports
  `NotDead`; a new store and worker continue the attempts (rule 8); a retry, give-up or
  release with a reclaimed fence is refused; a dead coalescing item, and one that ends dead
  after an enqueue while held, behave as PQ1 says.
- **Out of scope.** Logs, runs, scopes, retention.

### S10 · Logs and runs

**Track:** full · **Branch:** `epic-1/sprint-10-logs-runs` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S9.

- **Goal.** Log and run queues on PostgreSQL: payloads, keys while active (W2), log items
  deleted after success (PQ2), runs kept with their outcome.
- **Contract.** `EnqueueAsync`, `CompleteAsync`, `GiveUpAsync` and `RequeueAsync` for log
  and run items.
- **Acceptance.** Conformance, area `Shapes`: a log item's payload reaches its handler; two
  enqueues without a key are two items; with an active item's key the second is
  `Coalesced` with its id, and once the first completed a new one is `Added`; a log item is
  deleted after success; a run is kept finished with its attempts and times; a dead run
  gives up its key; a requeue of a dead run whose key is active, also one enqueued at that
  moment, reports `KeyActive`. The sample `LogAndRun` runs to the end; `docs/storage.md`
  says how an application reads its runs (PQ4).
- **Out of scope.** Scopes, retention.

### S11 · Scopes

**Track:** full · **Branch:** `epic-1/sprint-11-scopes` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S10.

- **Goal.** In a scoped queue on PostgreSQL no item is claimed while another of its scope
  is held, kept by the plain table `moltaro_work_scope` and its primary key, without
  advisory locks (rule 10, D9).
- **Contract.** `EnqueueAsync` with a scope, `ClaimAsync` for a scoped queue, and the
  fenced writes that end a hold.
- **Acceptance.** Conformance, area `Scopes`: two items of one scope are never held at
  once, also under many concurrent claimers over many scopes, which never fail with a
  deadlock; a batch takes at most one item per scope, and items of several scopes
  together; a held scope keeps no item of another scope from being claimed, even when its
  own waiting items are due earlier; after a completion the next item of the scope starts
  without waiting for an idle bound (W16); when a held item's lease ends, that item and no
  other of its scope is claimed; an item waiting behind a held scope costs no statement;
  items without a scope are not serialized; no scope row remains once nothing is held.
- **Out of scope.** Any order beyond the due time.

### S12 · Retention

**Track:** full · **Branch:** `epic-1/sprint-12-retention` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S11.

- **Goal.** Dead items and finished runs are deleted after the queue's retention, in
  bounded batches, by the worker, at the expiry the database tells it, without a signal.
- **Contract.** `DeleteExpiredAsync` of PostgreSQL; the worker deletes in batches of
  `RetentionBatchSize` until a batch is not full.
- **Acceptance.** Conformance, area `Retention`: a dead item and a finished run are deleted
  after their retention by the database's clock, and not before; a delete removes at most
  its batch, oldest first; a waiting or held item is never deleted, and a delete neither
  waits for nor removes an item that a requeue or a reviving enqueue holds locked; a worker
  deletes expired items at their expiry without a signal. Policy: the wait with an expiry
  before the next due time.
- **Out of scope.** A retention per state.

### S13 · Metrics

**Track:** full · **Branch:** `epic-1/sprint-13-metrics` · **Implementer:** Opus 5.5,
`high` · **Needs:** S12.

- **Goal.** Each worker reports concept §9's metrics through `System.Diagnostics.Metrics`,
  adding no statement.
- **Contract, fixed before Stage 3.** A meter `Moltaro.Work`, its name a public constant;
  per queue, tagged `moltaro.work.queue`: counters `moltaro.work.claims`,
  `moltaro.work.completions`, `moltaro.work.retries` and `moltaro.work.dead` in `{item}`;
  gauges `moltaro.work.depth` (`{item}`, waiting items) and `moltaro.work.oldest_due_age`
  (`s`), as of the worker's last look. If they need values in `WorkNextDue`, the owner
  approves that change with the stage.
- **Acceptance.** A `MeterListener` in area `Worker` sees each instrument with its tag after
  the event it counts; the idle statement count of S7 is unchanged.
- **Out of scope.** Tracing; log lines beyond S4's.

## Stage 4: the other providers

Its contract is written before S14, from what Stages 2 and 3 taught. The conformance suite
does not change for a provider.

### S14 · The SQL Server provider

**Track:** full · **Branch:** `epic-1/sprint-14-sqlserver` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S13 and the approval of Stage 4's contract.

- **Goal.** `SqlServerWorkStore` implements every operation with the SQL of SQL Server, and
  the whole conformance suite passes on SQL Server 2019 in CI.
- **Contract.** Every member of `SqlServerWorkStore`, with the facts of S3's brief: claims
  that skip locked rows under READ_COMMITTED_SNAPSHOT, and the enqueue of W10.
- **Acceptance.** Every conformance case passes; statements per item for claim and
  completion are reported.
- **Out of scope.** Versions before 2019.

### S15 · The MySQL provider

**Track:** full · **Branch:** `epic-1/sprint-15-mysql` · **Implementer:** Opus 5.5,
`xhigh` · **Needs:** S14.

- **Goal.** `MySqlWorkStore` implements every operation with the SQL of MySQL, and the whole
  conformance suite passes on MySQL 8.4 in CI.
- **Contract.** Every member of `MySqlWorkStore`, with the facts of S3's brief: no
  `RETURNING`, no partial index, next-key locks at REPEATABLE READ.
- **Acceptance.** As S14; besides, `Added` and `Coalesced` are told apart whatever the
  caller's connection options are, MySqlConnector's `UseAffectedRows` included.
- **Out of scope.** MariaDB.

## Stage 5: measured, explained, released

The planner completes these briefs from what the epic taught; they have no public contract.

### S16 · The measurements

**Track:** light · **Branch:** `chore/epic-1-measurements` · **Author:** Opus 5.5, `high` ·
**Needs:** S15.

- **Goal.** The figures of concept §9 per provider, from `Moltaro.Work.Measurements`, a
  console program on the public API, not packed and not a test project, started by hand.
- **Measured.** Statements per item for claim and completion in a batch, through the
  drivers' tracing; from a due time, and from a commit, to the start of the handler
  (targets: one second, one wake-up); statements of an idle queue (target: one per idle
  bound). Each figure names the processor, the system and the versions, never a host; the
  coordinator records them in `README.md`.
- **Out of scope.** Gating tests on figures.

### S17 · The explanations

**Track:** light · **Branch:** `docs/epic-1-explanations` · **Author:** Opus 5.5, `high`,
an agent with no other role in the epic · **Needs:** S16.

- **Goal.** In `docs/explanations`, as Markdown and as a self-contained HTML page each:
  E1 "How an item is claimed, leased and fenced" and E2 "How a worker wakes without
  polling" and E3 "The three shapes", animated, playing on their own and stepped through
  with a caption per step; E4 "How the library is built", a still figure of the packages
  and their dependencies.
- **Acceptance.** Both forms say the same and agree with the code; the reviewer checks
  them against the concept and `docs/storage.md`.

### S18 · The release of 0.1.0

**Track:** light · **Branch:** `chore/release-0.1.0` · **Author:** Opus 5.5, `high` ·
**Needs:** S17.

- **Goal.** Everything the owner needs to tag `v0.1.0`, as
  [branches and releases](branches.md#releases) says.
- **Acceptance.** `README.md` says what works, with the samples' use and the figures; what
  the epic delivered moves into the product documentation, and this document is removed;
  `<Version>` is `0.1.1-dev`; the open questions are gone through with the owner. The owner
  keeps the Trusted Publishing policy active and tags `v0.1.0`; the tag is never the
  coordinator's.
- **Out of scope.** Any change of behavior.
