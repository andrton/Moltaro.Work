# Documentation

Product documentation and active work are kept apart.

| Document or folder | What it holds | How it changes |
| --- | --- | --- |
| [`concept.md`](concept.md) | **The concept**: where the library comes from, its shapes and rules, providers, decisions D1–D16, non-goals and quality bar. The authority for what the library guarantees | A guarantee or a decision changes only with the owner's approval, in the same change as the code that follows it |
| [`storage.md`](storage.md) | **The provider contract and the schemas**: what each operation does to an item, what the tables mean, and what each database needs. PostgreSQL today; SQL Server and MySQL are written with the sprint that introduces them | With every change of a schema, together with its version |
| `explanations/` (planned) | **Explanations** for a reader: the architecture and how claiming, leases, fencing and retries work, each as Markdown and as a self-contained HTML page | As `AGENTS.md`, "Documentation", says |
| `images/` | **Pictures** that the documents show | With the document that shows them |
| [`work/`](work/README.md) | **Working documents**: what is being built now. Epics, their goals, requirements and open questions | Often, while the work is in progress. A working document describes intent, not delivered behavior |

## Rules

- **A document lives next to the code it changes.** Documentation of this library and of
  the packages in this repository belongs here. The plan and rationale for using the
  library inside another product belong to that product's repository.
- **This repository is public.** Nothing specific to a private product goes into it. A
  requirement that comes from a particular consumer is written here in general terms: what
  a consumer needs, not how that consumer is built.
- **A decision is not an implementation.** A document states plainly what exists and what
  is only planned.

## Working documents

- [Current work and epics](work/README.md)
- [Development process](work/process.md): roles, tracks, models and effort, sprints, the
  architect's review, refactoring, review and severity of findings
- [Branches and releases](work/branches.md): where work is committed, what `main` refuses,
  how a version is released, how GitHub and nuget.org are set up
- [Exceptions to the rules](work/exceptions.md): every place where the project departs from
  a rule of `AGENTS.md`, and why
- [Open questions](work/open-questions.md): what a sprint found and nothing answers yet
