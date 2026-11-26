# Development process

How an epic goes from a plan to finished code when the work is done by agents. The rules
every agent follows in every task are in [AGENTS.md](../../AGENTS.md); this document is for
whoever plans an epic, coordinates its sprints, reviews a sprint, reviews the architecture
or refactors.

This is the process of Moltaro.Lattice, Moltaro.Signals and Moltaro.OnecOutpost, carried
over on 26 November 2026. The owner's rules quoted below with their dates were made there
and hold here as well. One thing differs, by the owner's choice of 26 November 2026, as in
Moltaro: one implementer writes the contract, the tests and the code of a sprint, as three
commits in that order, and an independent reviewer checks the result. The accepted
decisions of this project are the decisions D1–D16 of the [concept](../concept.md). A decision that needs more than a row of its table gets a
record of its own in `docs/decisions`, created when the first such decision is.

## Three tracks

| Track | For | How |
| --- | --- | --- |
| **Full** | Changes to the core library and to the public contract | Everything below |
| **Refactoring** | The refactoring sprint that follows a merged sprint: the entries of the architect's list that were taken, with no change of behavior | [The architect's review](#the-architects-review) and [the refactoring sprint](#the-refactoring-sprint) |
| **Light** | Documentation, samples, build files, small fixes that change no behavior | One agent, the usual checks, and an independent review |

The coordinator picks the track for each sprint and the plan records it. When in doubt, full.

Every sprint, on any track, is reviewed by an independent reviewer, and the review
repeats after the fixes until it finds nothing that blocks or reaches its limit of rounds;
see [Review](#review).

## Roles

One agent holds one role in a sprint. The separation is the point: nobody checks their own work.

| Role | Does | Does not |
| --- | --- | --- |
| **Owner** (a person) | Approves the epic plan and every change of the public contract (the public API, the provider contract, the schemas of the providers, the names of metrics and log categories: everything a consumer of the library deals with); decides escalations; pushes and merges, or gives the coordinator a standing permission to | Approve internal contracts: the coordinator does |
| **Planner** | Writes the epic document: goal, scope, requirements, the sprints, and the contract of every sprint, stage by stage | Implement |
| **Coordinator** | Runs an approved epic sprint by sprint: assigns agents, runs the checks, reviews, decides disputes, keeps the epic document current; with the owner's standing permission, pushes and merges a closed sprint | Write production code or tests |
| **Implementer** | Puts the contract of a sprint into code and commits it: the public types and members the brief names, with their documentation, refusing until they are implemented. Then writes the tests from the brief and the contract and commits them. Then makes them pass and commits the implementation | Design the contract: a name or a signature the brief does not give goes back to the planner. Change the committed contract, or what a committed test asserts, without the coordinator |
| **Independent reviewer** | Reviews the result against the brief, with no part in producing it | Fix what it finds |
| **Architect** | After every merged sprint, reads what the system has become: the code the sprint touched and what it leans on, two or three sprints back, and once per stage the whole code base; returns a short list of what to simplify | Write code; propose a change of behavior or of the public contract |
| **Refactorer** | Carries out, in the refactoring sprint that follows a sprint, the entries of the architect's list that were taken | Change behavior, the public contract or what a test asserts; build for a future need; decide what needs the owner |

### Models and effort

Set the model and effort explicitly when starting a session or subagent, and check both
after it starts.

#### Claude Code

Every agent of a sprint uses Opus 5.5 (`claude-opus-5-5`), as the owner decided on
21 November 2026 after a run spent its weekly limit during a sprint. Claude Code effort
is capped at `xhigh` (the owner, 23 November 2026). The first two zones in
[Quality Priorities](../../AGENTS.md#quality-priorities) cover the decision component,
claiming, leases, fencing and the providers.

| Role | Effort |
| --- | --- |
| Independent reviewer, of code, of a document and of a refactoring sprint | `xhigh`, always |
| Planner and architect | `xhigh` |
| Implementer in the first two zones | `xhigh` |
| Coordinator, refactorer, study, fixes after review, documents and explanations, light-track author, and implementer in other zones | `high` |

#### Codex

Every agent uses GPT-6.2 Sol (`model = "gpt-6.2-sol"`). Set `model_reasoning_effort`
for the role.
[Codex configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference)

| Role | Reasoning effort |
| --- | --- |
| Planner, architect, and independent reviewer for any track | `ultra` |
| All other agent roles and assignments | `max` |

For either runtime, the planner names the effort of the implementer in each sprint brief
alongside its track. When reviews take more rounds or find more P1
findings, the coordinator reports it. In Claude Code, the planner raises the effort
within its cap.

## The plan

The planner delivers an epic document in the form described in [README.md](README.md),
with one brief per sprint. A brief has to be enough for an agent that knows nothing else:

- **Goal** — one behavior, end to end, small enough for one agent and one review.
- **Contract** — the public types and members the sprint adds or changes.
- **To read** — the decisions and documents the sprint depends on, with their dates.
- **Acceptance** — the behaviors the tests have to state. For a sprint whose goal names a
  performance target, also what is measured: a benchmark or a statement count. For a
  provider, the conformance suite on its real database.
- **Out of scope** — what the sprint deliberately leaves alone.
- **Track** — full, refactoring or light.

**Technical debt is looked for after every merged sprint.** It grows however carefully
each sprint is done, so the [architect's review](#the-architects-review) reads what the
system has become after each sprint is merged, and a
[refactoring sprint](#the-refactoring-sprint) of its own removes what was taken, without
mixing it into a change of behavior. The plan names an architect's review after each
sprint; a refactoring sprint exists only when something was taken.

**The first stage is the public contract, and nothing goes further until the owner approves
it** (the owner's rule of 26 November 2026). That stage creates the projects and packages
and writes the public .NET contract with its documentation:
- the queue declaration;
- enqueue;
- the handler;
- the options and results;
- the provider contract.

Samples, which compile and report where they stop, show how an application will use it.
No implementation is written in that stage.

**Then the thinnest path runs first.** After the contract, the plan orders the sprints so
that the thinnest path through the whole library runs as early as possible: one
coalescing item enqueued in a transaction, claimed and completed on PostgreSQL, a worker
woken through Moltaro.Signals. The sprints that harden and widen that path come after it:
- retries and dead items;
- logs and runs;
- scopes;
- retention;
- the other providers.

**A design document covers what its sprint builds.** `docs/storage.md` says what the
provider contract and the schemas are; a brief names the types its tests address. The
behavior of a later sprint is written in the brief of that sprint and proven by its tests,
not described ahead in prose.

**Until the first release the public contract is a draft.** The owner approves the
contract of a stage as before, but no consumer depends on it yet, so it does not have to
be perfect before code is written. A sprint that finds the contract should change makes
the change in its branch, lists it in the epic document for the owner and goes on; the
owner sees the list when the sprint closes and may overturn it. From the first release
on, every change of the public contract waits for the owner.

**The contract is written stage by stage.** An epic of many sprints is divided into
stages, and every sprint has its brief from the start. For the sprints of the first
stages, which the plan names, the brief gives the types and members exactly, with their
signatures. For a later stage
it says what the contract has to be able to do, and the planner writes the types and
members into those briefs before the stage begins, with what the earlier stages have
taught. The owner approves the public contract of each stage before its first sprint starts. A
sprint never starts from a brief whose contract is not written out and approved: an
interface designed twenty sprints ahead would be changed anyway, and every change of the
public contract goes through the owner.

For a sprint on a mechanism that has no public surface — the decision component, the
SQL of a provider — the contract is the internal types its tests address; they are named in
the brief and put into code in the same way. An internal contract is reviewed like any
other and approved by the coordinator; it goes to the owner only when it needs a change of
the public contract or a decision only the owner can make.

Before any sprint starts:

1. An independent reviewer reviews the plan the way a sprint is reviewed, and once more
   after the corrections, within the limit for a document ([Review](#review), "Rounds").
   A wrong plan is the most expensive mistake, and it is the cheapest to catch here.
2. The owner approves the plan, and with it the contracts of the first stages.

Before a later stage starts, the planner goes through the [open questions](open-questions.md)
and takes up those the stage touches. It then completes the contracts of its sprints, an
independent reviewer reviews them as the plan was reviewed, and the owner approves them. The coordinator does not start the first sprint of
a stage without that approval.

If a decision the plan depends on changes while the epic runs, the coordinator
stops and has the plan corrected before the next sprint.

## A sprint on the full track

1. **Contract.** The implementer adds the public types of the sprint with their
   documentation, exactly as the brief names them; what is not implemented throws
   `NotSupportedException`. The build is green. One commit.
2. **Tests.** The implementer writes the tests against the committed contract and marks
   those that cannot pass yet as skipped, with one shared reason. The sample of intended
   use is written too. The build is green and nothing fails. One commit.
3. **Implementation.** The implementer removes the skips and makes the tests pass. It may
   add tests. It does not change the committed contract, or what a committed test asserts,
   without the coordinator. If either looks wrong, it stops and reports to the
   coordinator. One commit.
4. **Verification.** The coordinator runs the checks itself: build, format, all tests, the
   samples. An agent's report that the checks passed is not a check.
5. **Review.** The coordinator reviews, and an independent reviewer reviews.
6. **Fixes.** The implementer fixes every P1, P2 and P3 finding. The next review covers
   only what changed and what was still open. Review and fixes repeat until an
   independent review ends with no P1, P2 or P3 finding that is still open, within the
   limit of rounds that [Review](#review) sets.
7. **Documents.** The coordinator compares `README.md`, the [concept](../concept.md),
   `docs/storage.md` and the explanations in `docs/explanations` with what the sprint
   delivered: what exists and what is planned, names, limits, described behavior. What
   no longer holds is corrected and committed with the sprint. A
   disagreement between the code and an accepted decision is not corrected in the text;
   it goes to the owner.
8. **Close.** With no P1, P2 or P3 open and the documents current, the coordinator marks
   the sprint done in the epic document. A finding is no longer open when an independent
   review found its fix sufficient, or when the owner accepted it or moved it to a named
   sprint, as [Review](#review) describes. The next sprint starts, with another implementer.

**Small changes during a sprint.** A change that comes up while a sprint is open and is
small — an answer of the owner that touches a rule, a document, a file of the
repository's settings or the brief of a later sprint; a sentence of documentation — is
made in the branch of that sprint and merged with it. It gets no branch and no pull
request of its own. The brief of the sprint or the [open questions](open-questions.md)
name it with the answer that caused it, whoever holds the role for that kind of work
makes it, and it is reviewed with the sprint. The light track is for a change made while
no sprint is open, or one that cannot wait for the open sprint.

**A sentence of documentation needs no step of its own.** When a decision that is already
made changes a sentence of public documentation or a comment and nothing else of the
contract — no name, no signature, no behavior that was not decided — the implementer
writes the sentence in the commit that carries the change it describes. The brief or the
register gives the wording, and the reviews check it. A change of a name, a signature or a
member remains a step of the contract commit.

**A decision is written in one place.** A decision of the owner, or an answer of the
planner, is written out once: in the decision or the list of decisions that owns
it, and in the brief of each sprint whose work it changes. Other documents that touch
the matter point to that place and do not restate it; a sentence elsewhere is changed
only when the decision makes it untrue. Tests are sized the same way: a rule that one
check enforces is stated by the smallest set of cases that tells a right implementation
from a wrong one, not once for every path that reaches the check.

When the last sprint of an epic closes, the same comparison is made once more over the
whole product documentation, and what the epic delivered moves from its working document
into it.

**Questions that do not block.** A sprint can find a gap that no brief, decision
or rule answers and that does not stop it. The agent that finds it reports it and does
not decide it. The coordinator enters it in the [open questions](open-questions.md), with
what the code does until it is answered, names the new entries in its report when the
sprint closes, and goes through all of them with the owner when the epic closes. An entry
that names a sprint as its limit is read when that sprint is prepared: the sprint does
not start before the entry is answered or moved to a later one. A question that does
block is not entered there: the coordinator stops and asks.

**A question does not hold a run.** A question blocks only when a wrong answer cannot be
undone cheaply: it is destructive, it leaves the repository, or it changes a released
contract. For any other question the coordinator takes the answer the planner
recommends, enters it in the open questions as taken, and goes on with the sprint; the
owner may overturn it. A run that the owner does not watch has its goal stated as a
result — "the codec of S2 with its tests green" — not as an activity, and the
coordinator's report says how far from that result it ended.

A committed test that turns out to be wrong is changed only with the coordinator's
agreement, with the reason in the commit message. Never by the implementer on its own.

**Edits that state nothing new.** A sprint can force edits on what already exists: a
member that was renamed is used under its new name, a descriptor is written in a new
form, a helper names another class. Such an edit is made by whoever makes the commit that
would otherwise break the build or a test, in that same commit, in tests and samples as well, so that the build stays
green after every step. When the commit of the contract removes a member in favour of
another, the new one works at once for what already exists, so that the tests that read
it still pass; this is the one case in which a member of a contract commit does more
than refuse. Such an edit never changes what a test asserts. The brief names each such
edit, and the reviewers check that nothing else was changed under this rule.

## The architect's review

The owner's rule of 23 November 2026, taken from Moltaro.Lattice.

- **Who.** The architect, an agent with no part in any sprint, read only. The coordinator
  of the next sprint starts it as its first task, once the previous sprint is merged.
- **What it looks at.** What the system has become, not a change: the code the merged
  sprint touched and what that code leans on, two or three sprints back, and once per
  stage the whole code base. It gets `AGENTS.md`, the concept, `docs/storage.md`, the
  epic document and the code, not the coordinators' reports. Reviewers look for defects
  in a change; the architect looks for what keeps the code from staying clean,
  understandable, easy to extend and free of duplicates.
- **What it asks.** What could be removed, merged or written more plainly without losing
  a guarantee or a measured figure; where one type or file has taken several
  responsibilities; where two places do the same thing; where the direction of
  dependencies between folders or projects is bent (principle 12); what a later sprint of
  the plan will find hard to extend, named with that sprint; where the code, the concept,
  `docs/storage.md` and `README.md` disagree; what a reader without the history cannot
  follow, and why.
- **KISS holds for a refactoring as for any change.** A refactoring that adds a type, an
  interface or a level of indirection has to remove more than it adds; one that only
  moves code around, or that a reader would not thank it for, is not proposed.
- **What it returns.** A short list, the most valuable first: the place, what is wrong in
  one sentence, the simpler form, what the change costs (files, risk, the tests that
  cover it), and which guarantee or measurement must be shown unchanged; then what it
  looked at and found sound. It proposes no change of behavior and none of the public
  contract; such an idea is a question for the planner.
- **What becomes of it.** The coordinator gives the planner the list as it is, with its
  view of each entry. The planner goes through it with the coordinator: each entry is
  taken, joined to a coming sprint that changes the same code, or refused with its
  reason.

## The refactoring sprint

After a sprint is merged and the architect's list is gone through, the entries that were
taken are carried out as a sprint of their own, before the next sprint of the plan, named
after the sprint it follows: S10aR after S10a. When nothing is taken, there is no
refactoring sprint.

1. **Refactoring.** The refactorer carries out the taken entries and nothing else: no new
   behavior and no fix of a defect. It changes no public member and no assertion of a
   test, and removes no test; it may make a test clearer. Each entry is its own commit,
   with the build and every test green after it. Where an entry touches a path with a
   measured figure, the figure is measured again and does not fall. An entry that turns
   out to need a change of the contract or of a decision is reported, not made.
2. **Verification, review, fixes, documents and close** as in a sprint on the full track,
   steps 4 to 8, with the refactorer in the implementer's place: it fixes the findings and
   may contest them. Its one reviewer, an independent reviewer, answers one question: is
   the code simpler to read, and is everything it guaranteed still guaranteed.

The brief of a refactoring sprint always says to the refactorer: KISS (`AGENTS.md`, "Keep
It Simple") holds here as everywhere. A change that makes the code larger, more abstract or
more general than today's case needs is not a refactoring.

## Review

**What the independent reviewer gets:** the sprint brief, the contract, `AGENTS.md` and the
diff. Not the implementer's explanation and not the coordinator's opinion: a reviewer that
is handed a conclusion confirms it.

**What is checked:**

- The behavior against the brief and the decisions it names.
- The architectural principles and the quality priorities of the zone, from `AGENTS.md`.
- That every behavior the brief states has a test, and that no test was weakened.
- That nothing outside the sprint's scope was changed. A small change that the owner
  decided while the sprint was open belongs to its scope when the brief or the
  [open questions](open-questions.md) name it; see "Small changes during a sprint".
- That the documentation the change touches says what the code now does.
- That every file the sprint added stands where `AGENTS.md` puts it — one type per file,
  a folder for one responsibility — in the tests and the samples as in the libraries. A
  folder that only collects files is a finding, whoever started it.

**A finding states** where it is, which rule, decision or requirement it breaks, and what
goes wrong because of it: a concrete failure or a concrete cost. Then its severity:

| Severity | Meaning | Blocks |
| --- | --- | --- |
| **P1** | Wrong result: a committed enqueue lost or a rolled-back one run; one item held by two holders at once; a write of a holder whose lease was reclaimed accepted; an item left due beyond its wake-up or idle bound; retention deleting an item that is not finished or dead; a guarantee of the concept or the contract broken; a security problem; the build or a test fails | Yes |
| **P2** | An accepted decision or an architectural principle is violated; a stated behavior has no test; a concurrency or lifetime hazard; the contract changed without approval | Yes |
| **P3** | It will cost later: duplication, unclear ownership or naming, missing or misleading public documentation, needless complexity, the quality priorities of the zone ignored | In code, yes; in a document, no ("Rounds") |
| **P4** | A suggestion or a matter of taste | No; listed for the coordinator |

A finding that names no rule and no failure is P4. A reviewer does not widen the scope:
"while you are here" is not a finding.

"Does not change the contract", said of the implementer, means
its names, signatures and behavior; a decided sentence of documentation is theirs to
write, as "A sentence of documentation needs no step of its own" says. "Every behavior
the brief states has a test" is ranked by what follows: a behavior the Acceptance
states and no test holds is P2; one the brief states elsewhere is P4.

A changed copy of the code that the tests do not notice is a finding of its own only when
the code is wrong, or when the change breaks a behavior that the Acceptance of the brief
states and no test holds it: then it is "a stated behavior has no test", P2. Where the
code is right and the behavior is not one the Acceptance states, it is P4. A review looks
for defects; it does not ask for a test of every line.

**Disputes.** The implementer may contest a finding, with its reason. The coordinator
decides. What the coordinator cannot decide goes to the owner.

**Rounds.** A sprint or a plan is never closed on its author's word that a finding was
fixed: the fix is reviewed. Every review has a limit of rounds, and reaching it is a
reason to stop and report, not to go on (the owner's rule of 21 November 2026, after ten
rounds on a protocol document of a sibling project that produced no code):

- **Code: three rounds.** Review and fixes repeat while an independent review finds a
  P1, P2 or P3, for at most three rounds. When the third still finds one, the coordinator
  stops and gives the owner the open findings in a few lines. The owner decides: fix
  again, accept, or move to a named sprint.
- **A document: two rounds.** This is a plan, a brief, an internal contract, the
  storage document or any other design document. Only a P1 or a P2 blocks it. A P3 that costs a
  sentence is fixed in the same pass; any other goes to the
  [open questions](open-questions.md). A P3 never causes another round. A round that
  finds no P1 and no P2 closes the review. After the second round the coordinator stops,
  and what is still open goes to the owner as a short list.
- **A document that grows is over-built.** When the fixes of a round leave a document
  longer than it was, the coordinator orders no further round. It has the document cut
  to what its sprint builds (`AGENTS.md`, "Keep It Simple"), and what was cut moves to
  the brief of the sprint that builds it.

When the fixes of one area of the code keep producing new findings, that area is
over-built in the same way: the coordinator has it simplified instead of fixed again,
and a simplification that changes the contract or a decision goes to the owner.

A finding that comes back is another matter. When a finding is found again after its fix
was reviewed, the coordinator settles it itself first: it decides the dispute between the
implementer and the reviewer, or gives the fix to another implementer. The coordinator
stops and asks the owner only when the finding comes back once more after that, or when
it can be closed only by changing the contract, the acceptance of the brief or an accepted
decision. Then it reports what keeps coming back and what it tried, and the owner
decides: the requirement changes, or the finding is accepted, or it is moved to a named
sprint. The decision is recorded in the epic document, and in the
[exceptions](exceptions.md) as well when it departs from a rule of `AGENTS.md`. A sprint is never closed with an
open P1, P2 or P3 on the coordinator's own word. The report of a closed sprint says how
many rounds it took.

## A study comes before a design

The first epic begins with one study, before the contract of its first stage is fixed
(the owner's rule of 17 November 2026, applied once per epic here):
- how established work queues claim, lease, fence, retry, give up and clean up;
- how they wake their workers;
- what SQL each database needs for this.

The candidates are River, Oban, graphile-worker, Hangfire and Quartz.NET. A later sprint
that builds a mechanism the study did not cover begins with a study of its own.

The agent reads the source, not summaries, of two to four established systems that solve
the same problem, at pinned commits, the licence checked first by the rule on sources
below. It reports, briefly: what
each guarantees, what it costs, what this library takes and why, what does not fit and why. The
planner and the coordinator read the report before the contract step ends. A design of our
own is made only where nothing fits, and the report says so. The study is bounded: it
answers the questions the brief names and is not a survey. It is a step of its sprint, or
of the plan for the study of the first epic, not a sprint of its own.

## Code modelled on another project

When a sprint repeats a proven solution, its code is modelled only on sources under a
permissive licence (MIT, Apache-2.0, BSD). Sources under GPL or LGPL may be read about
in their papers and documentation to understand an idea; their code is not opened while
ours is written, and nothing is ported from it. A type that follows the code of a source
closely enough to be an adaptation of it says so in a comment at the head of its file —
which project, which file, which commit — and `THIRD-PARTY-NOTICES.md` gains the
project's copyright line and licence text in the same change. The pull request lists
what was adapted and what was only taken as an idea.

## Commits

- A sprint on the full track is a short series of commits in this order: contract, tests,
  implementation, fixes; a refactoring sprint is its refactoring steps, then fixes. On the
  branch they are not squashed, so that "tests first" stays visible in the pull request;
  the merge squashes them into one commit of `main`.
- The subject names the epic and the sprint: `Epic 1, sprint 3: tests for refusing a completion whose fence was reclaimed`.
- No agent attribution lines, as `AGENTS.md` says.
- Commits stay local. Nothing is pushed without the owner's instruction or standing
  permission.
- One branch per sprint, cut from `main` and merged back through a pull request when the
  sprint closes. [Branches and releases](branches.md) has the names, the merge rules and
  what `main` refuses.

## When to stop and ask the owner

- The public contract has to change, from the first release on. Before it, the change
  is made and listed, as "Until the first release the public contract is a draft" says.
- A decision of the concept contradicts the brief, or has changed.
- A finding keeps coming back after the coordinator has settled it once, as
  [Review](#review) describes.
- A review has reached its limit of rounds: three for code, two for a document.
- A dispute the coordinator cannot settle.
- Anything destructive, or anything that leaves this repository: a push, a pull request,
  a merge, a release tag. A standing permission of the owner, as
  [branches and releases](branches.md#who-does-what) describes it, covers the push, the
  pull request and the merge of a closed sprint while no decision of the owner is open;
  a release tag needs the owner's word for that version.

A measured fall of performance is not by itself a reason to stop (the owner, 16 November
2026). First the cause is found, with a control or a profile. Then it is compared with
what established systems do for the same problem: is this what the problem costs
everywhere, or is something done wrong here. Then alternatives are tried, each kept only
if a figure justifies it. The owner is asked only when nothing helped, the fall cannot be
put down to what the problem costs in the industry, and a requirement of the project is
missed; the question then comes with the cause, the evidence, the alternatives tried with
their figures and the requirement that is missed. A fall that has a known cause and stays
within the requirements is reported at the close.

## Where the state lives

In the epic document: the list of sprints with the status of each. The coordinator updates
it when a sprint closes. What a later agent needs to know is in the brief of its sprint.
What nobody has answered yet is in the [open questions](open-questions.md). There are no
other reports, logs or handoff files.
