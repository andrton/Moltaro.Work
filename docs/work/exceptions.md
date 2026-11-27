# Exceptions to the rules

[AGENTS.md](../../AGENTS.md) owns the rules of the project. This document records every
place where the project departs from one of them: which rule, where, why, and until when.
It exists so that a departure is a decision somebody can find and question later, not
something a reader has to guess from the code.

## How an exception is recorded

- An exception is approved by the owner before it is used.
- It is entered under **Approved, not in force yet** when the owner approves it, moved to
  **In force** by the change that introduces the departure, and moved to **Ended** by the
  change that ends it.
- An entry is numbered `X1`, `X2`, … and keeps its number for good. It names the rule in
  the words of `AGENTS.md`, the exact place of the departure, the reason the rule cannot be
  followed there, and what keeps the departure small.
- A temporary exception names the sprint or the event that ends it.

An entry is a table:

| | |
| --- | --- |
| **Rule** | The rule, in the words of `AGENTS.md` |
| **Departure** | Where exactly the project departs from it |
| **Why** | Why the rule cannot be followed there |
| **Kept small by** | What bounds the departure |
| **Approved** | By whom and when |
| **Since** | The change or sprint that introduced it |
| **Until** | The sprint or event that ends it, or "Permanent" |

## In force

### X1. The connection strings of the CI service containers

| | |
| --- | --- |
| **Rule** | "No secrets, tokens, machine paths, connection strings, build output or run output in Git." |
| **Departure** | `.github/workflows/ci.yml` and `.github/workflows/publish.yml` name the connection strings of their own PostgreSQL, SQL Server and MySQL service containers |
| **Why** | The tests of each provider need their real database, and the passwords of these containers exist only inside the job; a repository secret would protect nothing |
| **Kept small by** | Only those two files, only their own service containers, with throwaway passwords; nothing else in the repository names a connection string |
| **Approved** | By the owner, 27 November 2026, as the answer to PQ5 of the Epic 1 plan |
| **Since** | Epic 1, S2 |
| **Until** | Permanent |

### X2. A suppressed warning on `WorkWorker.Failed` until S7

| | |
| --- | --- |
| **Rule** | "C# on .NET 10, nullable enabled, warnings are errors." |
| **Departure** | `#pragma warning disable CS0067` and its `restore` around the event `Failed` in `src/Moltaro.Work/WorkWorker.cs` |
| **Why** | The event is part of the contract of S1, but nothing raises it before S7 implements `RunAsync`. Until then the compiler reports CS0067, "the event is never used", and the build fails |
| **Kept small by** | One warning, on one member, with a comment that names S7 |
| **Approved** | By the owner, 27 November 2026 |
| **Since** | Epic 1, S1 |
| **Until** | Epic 1, S7, which raises `Failed` and removes the pragma |

## Approved, not in force yet

None.

## Ended

None.
