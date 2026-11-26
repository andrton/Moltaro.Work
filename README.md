# Moltaro.Work

Durable background work for .NET on PostgreSQL, SQL Server and MySQL: enqueue in your own
transaction, coalesce work per key, claim with leases and fencing, retry with backoff and
dead letters, clean up by retention, wake at once with
[Moltaro.Signals](https://github.com/andrton/Moltaro.Signals).

## Status

Concept stage. The [concept](docs/concept.md) is agreed; no code is written yet, and
nothing below works today. The first epic, the library up to its first release, is
planned in [current work](docs/work/README.md); its first stage is the public .NET
contract.

## What it is, and what it is not

An application keeps its background work in its own database: jobs to run, messages to
deliver, records to recompute. With this library it declares a queue, enqueues items in the
same transaction as the write that creates them, and writes a handler. The library does the
rest:

- an item exists if and only if the business write commits;
- items come in three shapes:
  - coalescing: one item per key, however often it is enqueued;
  - log: every item counts;
  - run: kept with its outcome for its history;
- workers claim items in batches under a lease, and a holder whose lease was reclaimed
  cannot complete the item;
- failures are retried with a backoff and given up after a limit, with the reason kept;
- every queue cleans up after itself;
- workers wake through Moltaro.Signals when work is committed and at the earliest due
  time, never on a polling interval.

It is not a message broker, a scheduler of cron expressions, a workflow engine or a
dashboard. See the [concept](docs/concept.md#7-non-goals-and-possible-later-work) for the
full list of non-goals.

## Packages (planned)

| Package | Contents |
| --- | --- |
| `Moltaro.Work` | The database-neutral core |
| `Moltaro.Work.PostgreSql` | The PostgreSQL provider |
| `Moltaro.Work.SqlServer` | The SQL Server provider |
| `Moltaro.Work.MySql` | The MySQL provider |
| `Moltaro.Work.Hosting` | Registration in a .NET generic host |

They will be published on [nuget.org](https://www.nuget.org) by the
[Publish](.github/workflows/publish.yml) workflow when a version tag is pushed. Nothing is
published yet. [Branches and releases](docs/work/branches.md) describes how work reaches
`main` and how a version is released.

## Documentation

- [Concept](docs/concept.md): where it comes from, shapes and rules, providers, decisions
  and quality bar.
- [Documentation index](docs/README.md) and [current work](docs/work/README.md).
- [How the project is built](AGENTS.md): architectural principles, code style and working
  rules, for people and coding agents.

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). Run the commands
from the `src` directory.

```bash
dotnet build
```

```bash
dotnet test
```

## License

[Apache License 2.0](LICENSE)
