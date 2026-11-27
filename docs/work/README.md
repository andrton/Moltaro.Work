# Current work

The work is organized in epics. An epic is a result that is useful on its own; it is
delivered over several sprints.

| Epic | Result | Status |
| --- | --- | --- |
| 1. First release | The library as the [concept](../concept.md) describes it: the core, the PostgreSQL, SQL Server and MySQL providers and the hosting package, published on nuget.org as version 0.1.0 | In progress, by the plan in [epic-1-first-release.md](epic-1-first-release.md), which the owner approved on 27 November 2026 with the contract of its first stage |

## The repository as it is set up

- `src` holds the build settings and the solution
  [`src/Moltaro.Work.slnx`](../../src/Moltaro.Work.slnx), which lists the projects created so
  far. Each later sprint adds the projects that `AGENTS.md` lists as planned when it first needs
  them.
- `src/global.json` names Microsoft.Testing.Platform as the test runner, for xUnit.net v3.
- CI runs the tests of each provider on its database, PostgreSQL, SQL Server or MySQL, as a
  service container.
- The rules of `main` and of release tags are files in `.github/rulesets`. GitHub applies
  them only after the owner does, as [branches and releases](branches.md#setting-it-up-on-github)
  describes.
- Publishing uses nuget.org Trusted Publishing. The owner creates its policy once, as
  [branches and releases](branches.md#setting-it-up-on-github) describes.

How an epic is planned, carried out sprint by sprint and reviewed is described in the
[development process](process.md). Where the commits go, what `main` refuses and how a
version is released is in [branches and releases](branches.md). Every place where the
project departs from a rule of `AGENTS.md` is recorded, with its reason, in
[exceptions to the rules](exceptions.md). A question that a sprint finds and that nothing
answers yet is written down in the [open questions](open-questions.md), and they are gone
through with the owner when an epic closes.

## How an epic document is written

One file per epic, `epic-<number>-<short-name>.md`, with these parts:

1. **Goal** — the result and who it is for.
2. **Scope** — what is included and what is deliberately left out.
3. **Requirements** — numbered `R1`, `R2`, …; each says what is needed, what needs it and
   whether it exists today. Other documents refer to them as "Epic 1 R3".
4. **Acceptance** — how we know the epic is done.
5. **Open questions** — decisions that are not made yet, each with the options.
6. **Sprints** — the order of work, without dates, grouped into stages. An epic that is
   ready to be carried out has one brief per sprint, as the
   [development process](process.md#the-plan) describes, and the status of each sprint.
   Before that, an outline of stages is enough.

A requirement level is one of:

- **Must** — the epic cannot be finished without it.
- **Should** — needed for a good result; a documented workaround is acceptable at first.
- **Release** — not needed to develop and test, but needed before anybody relies on it.

When an epic is finished, what it delivered is described in the product documentation, and
its working document is removed from this folder.
