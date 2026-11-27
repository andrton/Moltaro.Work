# Repository Guidelines

`AGENTS.md` is the one instruction file for coding agents and a short statement of how
this project is built. Shared rules live here; `CLAUDE.md` only points to this file.

## Project

Moltaro.Work is a .NET library for durable background work stored in the application's
own relational database:
- **A database-neutral core** owns the rules: claiming, leases, fencing, retries, dead items,
  retention and waking.
- **A provider package per database** holds its SQL and its schema: PostgreSQL, SQL Server
  and MySQL.
- **A hosting package** registers queues and workers in a .NET generic host.

An application enqueues an item in its own transaction and writes the item's handler;
the library does the rest. Workers wake through Moltaro.Signals and at the earliest due
time, never on a fixed polling interval.

What exists today is the **concept** and the **public contract of the core**, the package
`Moltaro.Work`, documented: its types check their arguments and options, `WorkClient.Join`
and `WorkTransaction.EnqueueAsync` hand a checked enqueue to the provider, and every other
entry point throws `NotSupportedException`. Nothing else is implemented yet. The
[concept](docs/concept.md) is the authority for what the library guarantees: its shapes,
its rules, its decisions D1–D16 and its non-goals. `README.md` says what works today; a
feature is delivered when the README says so.

This repository is public. Nothing specific to a private product goes into it: no names
of its modules, tables or customers, in code, tests, samples or documents.

## Layout

| Path | Contents |
| --- | --- |
| `src` | The solution, every project and the build settings. Run `dotnet` from here. The solution holds the projects created so far |
| `docs/concept.md` | Where the library comes from, its semantics, decisions, non-goals and quality bar |
| `docs/storage.md` | (planned) The provider contract and the schema of each database, written with the sprint that introduces them |
| `docs/explanations` | (planned) Explanations of the architecture and of how the library works, each as Markdown and as HTML |
| `docs/images` | Pictures that the documents show |
| `docs/work` | Working documents: the epics in progress, the development process, branches and releases, exceptions to the rules, open questions |

Each project is created by the sprint that first needs it, under the name the plan of the
first epic fixes. The packages planned by the concept:

| Project | Contents |
| --- | --- |
| `Moltaro.Work` | The core: the public contract, the pure decision component, the worker loop. Depends on nothing outside .NET but Moltaro.Signals |
| `Moltaro.Work.PostgreSql` | The PostgreSQL provider: its SQL and its schema script |
| `Moltaro.Work.SqlServer` | The SQL Server provider |
| `Moltaro.Work.MySql` | The MySQL provider |
| `Moltaro.Work.Hosting` | Registration in a .NET generic host and a container; a thin package |
| `Moltaro.Work.Samples` | Runnable samples of intended use |
| `src/*.Tests` | Tests, one project per library; the provider conformance suite is shared by the provider test projects |
| `Moltaro.Work.Measurements` | The program that measures the performance targets of the concept. Started by hand; never shipped and not a test project |

The repository root holds only repository-level files. Code never lives there.

## Keep It Simple

KISS is the base rule for design, code, tests and documentation.

- Start from the simplest thing that fully solves today's case. Prefer one plain rule or
  an existing mechanism over a new one.
- Build what is needed now. What is not needed yet is not built; at most it is noted as
  planned in the document that owns it.
- No speculative abstractions, options, switches, retries, fallbacks or defensive checks
  "just in case".
- When something is awkward to use, fix its cause. Do not wrap it: no adapter or facade
  class around a core type to work around what the core lacks.
- Simple to use comes first: a small public surface, few options, one obvious way to do
  each thing. Then a simple implementation.
- A case that is rare in practice and costs no guarantee (see "Quality Priorities") is not
  handled. Its limitation is stated in one sentence, and nothing is built for it.
- A review finding is fixed by the smallest change that removes it. A new mechanism,
  option, state or public type is added only when a guarantee needs it; otherwise the fix
  states the limitation or removes the cause. An area whose fixes keep producing new
  findings is over-built: simplify it instead of fixing it again.

Simple is not the goal by itself. Do not make something simpler by dropping a guarantee,
blurring a boundary or pushing the difficulty onto the caller. The target is the simplest
design that keeps every accepted guarantee. When simple and correct disagree, correct
wins. When simple and fast disagree, measure before choosing.

The value of this library is that a team can read it and trust it with its background
work. A request outside durable background work is declined, or noted as planned in the
concept.

## Architectural Principles

1. **Work lives with the data that creates it.** An item is enqueued through the caller's
   connection and transaction (ADO.NET) and exists only if that transaction commits. The
   library opens no transaction of its own around the caller's write.
2. **At least once, never twice at the same time.** A handler may run again after a lost
   lease, so handlers are idempotent. Two holders never hold the same item, and every
   write of a holder is fenced: a holder whose lease was reclaimed cannot complete the
   item.
3. **State lives in the database, not in memory.** Due times, leases, attempts, retry delays
   and dead items are columns of the item. A process can stop at any moment and nothing
   it knew is lost.
4. **Nothing grows forever.** Every queue has a retention, and deletion runs in bounded
   batches.
5. **No polling.** A worker waits for its queue's signal or its earliest due time, with an
   idle bound for a signal lost in a crash. A statement sent while nothing is due is a
   defect.
6. **The core knows no database.** SQL lives only in providers. Every provider passes the
   same conformance suite against its real database. A behavior that one provider cannot
   keep is not a behavior of the library.
7. **No advisory locks.** The scope rule and the fence are kept with plain tables, row
   locks and unique constraints, the same way in every provider.
8. **Decisions are pure.** What may be claimed, when to retry, when to give up and when to
   delete is decided by a pure component that takes the time as input. It is tested
   deterministically, with no database.
9. **The database's clock decides.** Due times and lease ends are compared in SQL with the
   database's time, never with the clock of a worker.
10. **Waiting is finite and outside locks.** No lock is held across an `await` of the
    application's handler. Every wait honors cancellation; a wait that can be long has a
    deadline. A timeout or a limit an application can observe — lease length, heartbeat
    interval, batch size, attempt limit, backoff, retention, idle bound, lanes — is an
    option with a default, validated when the queue or worker is created; it is never a
    constant in the code, unless a column of the schema fixes it — the length of a queue
    name, a key, a scope or a reason: such a limit is part of the schema, stated in
    `docs/storage.md`, and changes only with the schema's version.
11. **Never an approximate success.** A request is honored or refused with a code. An
    enqueue that did not reach the transaction, a completion refused by the fence and an
    item given up are distinct outcomes.
12. **Dependencies point one way.**
    - The core depends on nothing outside .NET but Moltaro.Signals.
    - A provider depends on the core and its database driver: Npgsql,
      Microsoft.Data.SqlClient or MySqlConnector.
    - The hosting package depends on the core and the .NET hosting abstractions.

    Nothing depends on a product that uses the library. Inside an assembly a folder is a
    namespace and one responsibility, and folders depend on each other in one direction:
    the decision component knows nothing of SQL or of the worker loop.
13. **Performance is a design goal, proven by measurement.** Statements per processed item
    and the time from a due time to the start of a handler are measured per provider.
    Added complexity is justified by a number.

## Quality Priorities

When two good things pull apart, the zone of the code decides which one wins. Read each
row left to right: an earlier quality is not traded for a later one.

| Zone | Priorities, in order |
| --- | --- |
| The decision component, claiming, leases and fencing | Correctness → simplicity → speed |
| Providers: SQL and schema | Correctness under concurrency → statements per item and latency → readability |
| Public contract: queue declarations, enqueue, handlers, options, results, the provider contract, metric names | Correctness → clarity of use → stability → speed |
| Hosting, samples, tests, measurements | Readability → simplicity; speed does not matter |

- **Correctness is first everywhere.** For this library it means:
  - a committed enqueue is never lost, and a rolled-back one never runs;
  - two holders never hold the same item at once;
  - a holder whose lease was reclaimed never completes the item;
  - an item never stays due longer than its wake-up or its idle bound;
  - retention never deletes an item that is not finished or dead.
- **A performance claim needs a measurement.** "Faster" or "fewer statements" is accepted
  with a number from the measurement program or a statement count, not by argument.
  Without one, the simpler code stays.

## Design Patterns

Patterns are vocabulary for explaining a decision, not a requirement to meet. No pattern
is applied because it exists.

- Put logic where the data it needs lives.
- Introduce an interface only at a real boundary: the public contract, the provider. One
  implementation behind a private interface is not a boundary.
- A known pattern is welcome when it removes real duplication or names a real variation.
  Say which problem it solves; if that cannot be said in a sentence, leave it out.

## The Contract Comes First

A feature starts as contract: public types with documentation, a sample of its use, and
tests that state its behavior. Implementation follows.

- **The first stage of the first epic is the public .NET contract and nothing else:**
  - the projects and packages;
  - the queue declaration;
  - enqueue;
  - the handler;
  - the options and results;
  - the provider contract.

  Samples show how an application will use it. The owner approves this contract before
  any implementation starts.
- **Respect the stage that was agreed.** "In the contract, not implemented yet" means
  exactly that: the entry points throw `NotSupportedException`, the tests that need the
  feature are skipped with a shared reason, the sample is written and reports where it
  stops.
- **Checks that need no implementation belong to the contract and work from the start**,
  for example validating a queue name or an option.
- **The implementation passes the tests written for the contract.** Do not weaken or
  delete a test to get a green run.
- **No compatibility shims.** Versions stay 0.x until a first production adopter runs on
  the library. When something is replaced, the old code, tests and documentation are
  removed in the same change.
- **The schema of each provider is part of the contract.** It lives in `docs/storage.md`
  and in the provider's script, written with the sprint that introduces it, and carries a
  version.

## Code Style

Language and build:

- C# on .NET 10, nullable enabled, warnings are errors. `dotnet format --verify-no-changes`
  stays clean.
- Package versions are managed centrally in `src/Directory.Packages.props`; a
  `PackageReference` has no `Version`.
- SQL is written for its database in its provider, as plain parameterized commands. No ORM
  in the library.

Organization:

- One type per file, named after the type. The generic form of a type that also has a
  non-generic one goes to `Name.Generic.cs`; a nested type with a file of its own goes to
  `Outer.Nested.cs`.
- A folder is a namespace and one responsibility. No `Internal`, `Helpers`, `Utils` or
  other folders that collect unrelated types.
- `internal` by default. A type is public only when it is part of the contract.

Naming:

- Public types are named in the contract stage and reviewed as public API. A name that
  would be generic on its own, such as `Queue` or `Item`, takes the `Work` prefix.
- Every enum type ends with `Enum`.
- Asynchronous methods end with `Async` and take a `CancellationToken` last, when a
  caller awaits them; a private loop that ends when its worker stops takes none.
- Private fields are `_camelCase`.
- Name things for what they do for the user, not for the framework mechanism inside.
- Use the words of the concept: *queue*, *item*, *key*, *scope*, *due time*, *lease*,
  *fence*, *attempt*, *dead item*, *retention*, *coalescing*, *log*, *run*, *provider*.

Shape of the public API:

- Something that answers at once from local memory is synchronous. Something that can
  wait is asynchronous.
- **Expected outcomes are results, not exceptions:** a refused completion, a dead item, an
  enqueue that coalesced. Exceptions are for failures:
  - `ArgumentException` and its relatives report a caller's mistake;
  - `ObjectDisposedException` follows a dispose;
  - `NotSupportedException` marks what is not implemented.
- **Every public member has XML documentation:** what it does, what it guarantees, what it
  does not, and which errors it reports. Plain sentences, no restating of the signature.
  Internal members are commented with `//`, not `///`, so that the XML documentation of a
  package holds only its public contract.

Writing code:

- Write for a reader. Descriptive names, one statement per line, blank lines between
  steps; no nested ternaries or dense chains.
- Keep a method focused, without splitting a simple operation across needless helpers.
- Comments are in English and explain why: an invariant, an ordering requirement, who
  owns mutable data, what a lock protects. They do not repeat the code.
- Never return an invented success in place of a failure.
- No reflection on a call path. No duplicated checks. No redundant models.

## Tests And Samples

- A public behavior has a scenario test that reads like its use. A test name is a
  sentence stating the behavior.
- **Tests use the public API.** The decision component has no public surface and is tested
  directly, through `InternalsVisibleTo` for its test project. Each such test states a rule
  of the concept, never how the code happens to be built.
- **Every rule of the concept's section 3 has a deterministic test**, of the pure component
  where .NET decides and of the conformance suite where SQL decides, and rule 12 is held by
  the review of the SQL.
- **Time is injected through `TimeProvider`.** A test waits for an observed event; it never
  sleeps and never polls with a delay.
- **The conformance suite is written once**, against the provider contract, and run by
  each provider's test project against its real database: PostgreSQL, SQL Server and
  MySQL, in CI as service containers and locally in containers. It includes the
  concurrency cases:
  - many workers claiming at once;
  - a worker that dies holding an item;
  - a holder that writes after its lease was reclaimed;
  - an enqueue in a rolled-back transaction.

  A provider is never tested against an in-memory substitute.
- **A test project is organized like a library:** a folder is a namespace and one area of
  behavior. Code that two or more test projects need lives once, in a
  `Moltaro.Work.TestSupport` project, created when the second test project needs it; it is
  never shipped and runs no tests of its own. The conformance suite is a set of abstract
  test classes in its folder `Conformance`, which run only in the classes each provider's
  test project derives from them.
- **A feature that is not implemented has its tests written and marked
  `[Fact(Skip = ...)]`** with one shared reason. Skipping is never used for a flaky test.
- **The contract and the tests come before the implementation.** On the full track of the
  [development process](docs/work/process.md), the implementer commits the contract of a
  sprint, then its tests, then the implementation, as three commits. An independent
  reviewer checks the result. A committed test changes its assertion only with the
  coordinator's agreement, and the commit says why.
- **Samples and README code.** Every sample runs to the end with `dotnet run`. Code shown in
  `README.md` must compile against the current contract.

From `src`:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Documentation

- Everything in the repository is written in English: documentation, code comments, XML
  documentation, test names and commit messages. This holds whatever language the task
  was given in.
- A document lives next to the code it changes. Product documentation is in `docs`;
  working documents, the epics, are in `docs/work`. The plan for using this library inside
  another product belongs to that product's repository.
- A decision is not an implementation. Say plainly what exists and what is planned.
- **Explanations are written twice.** An explanation of the architecture or of how the
  library works lives in `docs/explanations` as Markdown and as HTML, saying the same
  thing; the two change together, in the same change. The HTML is one self-contained file
  that opens from disk, without a build, a server or anything loaded from elsewhere. In the
  main cases, named when the explanation is planned, the HTML is animated: the animation
  plays on its own, and the reader can step through it, with a caption for each step.
- **When behavior changes, update the document that owns it in the same change**, and the
  explanations that describe it, both forms:
  - the concept for semantics and decisions;
  - `docs/storage.md` for the provider contract and the schemas;
  - `README.md` for status and use.
- **When a larger block of work is finished** — a sprint, an epic, a change of the public
  contract — compare `README.md`, the concept, `docs/storage.md` and the explanations with
  what now exists. Correct every statement of status, every name, limit and described
  behavior that no longer holds, in the same change.
- That check never changes a guarantee. When the code and an accepted decision disagree,
  it is a finding to report, not a sentence to edit.
- Requirements that come from a particular consumer are written in general terms.

## Working Rules

- The user's instructions define the task. Among repository documents this file owns the
  global rules. Code and tests show what the code does, not that it is intended.
- A departure from a rule of this file is recorded in
  [exceptions to the rules](docs/work/exceptions.md) in the same change: which rule,
  where, why and until when.
- Before designing a change to the contract, read the concept section that covers it.
- Make routine, reversible choices within the task. Ask only when a missing decision
  changes behavior, the contract or the scope. Say what was left out.
- Review the whole affected flow before finishing: contract, implementation, tests,
  samples, `README.md`, `docs`.
- Keep plans, progress and findings in the conversation. Do not create report, audit or
  plan files unless the user asks for that document.
- Never revert the user's changes or unrelated work.
- Do not commit or push unless asked. Stage only the files of the task. No secrets,
  tokens, machine paths, connection strings, build output or run output in Git.
- Never commit to `main`. Work on a branch named as
  [branches and releases](docs/work/branches.md) says. `main` changes only through a pull
  request, merged by the owner or, for a closed sprint and with the owner's standing
  permission, by the coordinator of an epic. Never move a release tag; create one only
  when the owner tells you to, for that version.
- Commit messages carry no agent attribution: no `Co-Authored-By` line and no "generated
  with" line. This overrides the default of any tool.
- Planning an epic, coordinating its sprints, reviewing a sprint, the architect's review
  or refactoring follows the [development process](docs/work/process.md). Read it before
  taking one of those roles.
- Technical debt is found by the architect's review after every merged sprint and removed
  in the refactoring sprint that follows it, as the development process describes. Its
  measure is this file.
- Finish by reporting what changed, which checks ran and what is not done. When the task
  leaves changes in Git, suggest a commit message.
