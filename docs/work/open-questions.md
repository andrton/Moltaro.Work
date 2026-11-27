# Open questions

Questions that came up while a sprint was carried out and that no brief, decision or rule
answers. Each is written down when it is found, so that it is not settled by whoever
happens to write the code, and not forgotten.

A question that blocks a sprint does not wait here: the coordinator stops and asks the
owner, as the [development process](process.md#when-to-stop-and-ask-the-owner) says. This
register is for what does not block: the sprint goes on, and the entry says what was done
in the meantime.

## How it is used

- **Who writes.** The coordinator, when an agent or a reviewer reports a gap, or when it
  finds one itself. An agent reports; it does not decide.
- **What an entry says.** Where the question was found, the question itself, what the
  code or the tests do until it is answered, and who has to answer it.
- **When it is looked at.** When a sprint closes, the coordinator names the entries the
  sprint added in its report to the owner. When the coordinator prepares a sprint, it
  reads the open entries that name that sprint and does not start it before each of them
  is answered or moved to a later sprint by whoever decides it. Before the contracts of a
  stage are written, the planner goes through the open entries and takes up those the
  stage touches. When an epic closes, every entry is gone through with the owner: none
  stays open without a decision to carry it into the next epic.
- **How it ends.** An answer comes from whoever the entry names as deciding. A rule of
  the public contract or a guarantee of the concept is the owner's; what follows from a
  rule that already stands the planner answers and reports to the owner. It is entered
  here with its date and with who gave it, and the rule it gives goes where it belongs:
  the concept, `docs/storage.md`, the epic document, a brief, `AGENTS.md`. The entry then
  moves to **Answered** and says where the rule now lives.

An entry is numbered `Q1`, `Q2`, … and keeps its number for good:

| | |
| --- | --- |
| **Found** | When, and in which sprint or review |
| **Question** | The gap, stated so that it can be answered |
| **Until answered** | What the code and the tests do meanwhile |
| **Decides** | The owner or the planner |

## Open

### Q1. May the source of a project under MPL-2.0 be read?

| | |
| --- | --- |
| **Found** | 26 November 2026, in the study of Epic 1 |
| **Question** | The [rule on sources](process.md#code-modelled-on-another-project) names permissive licences (MIT, Apache-2.0, BSD), whose code may be read and modelled on, and GPL or LGPL, whose code is not opened. River is under MPL-2.0, which the rule does not name. May its source be read, in a study or as a model? |
| **Until answered** | Taken as GPL/LGPL: River was studied from its documentation only, and no code of ours is modelled on its source |
| **Decides** | The owner |

### Q2. A due item that cannot be taken hides the later due times of its queue

| | |
| --- | --- |
| **Found** | 27 November 2026, in round 2 of the review of the Epic 1 plan, a P2 left open when the review reached its limit of rounds |
| **Question** | W7 of the [plan](epic-1-first-release.md#where-the-concept-is-silent) has a worker whose claim takes nothing, although the database said something was due, wait for the signal or the idle bound. A due item that cannot be taken — behind a held scope, or locked by an open transaction — then keeps `UntilClaimable` at zero, and an item that falls due later, of another scope, starts at the next signal or idle bound instead of at its due time: against D7 and the one-second target of concept §9. The reviewer proposed: the next claimable time leaves out waiting items whose scope is held; W7's wait applies only after a look that returned zero, and a claim at a positive deadline that takes nothing looks again; S11 gets the case "an item of another scope that falls due while a held scope's item waits starts at its due time". Is this fix taken now, with a review of it alone, or when the contract of Stage 2 is written? |
| **Until answered** | The plan keeps W7 as written. No sprint of Stage 1 implements it; the contract of Stage 2 is not written before the answer |
| **Decides** | The owner |

### Q3. The statements of handing on the signal

| | |
| --- | --- |
| **Found** | 27 November 2026, by both reviewers in round 2 of the review of the Epic 1 plan |
| **Question** | W16 of the [plan](epic-1-first-release.md#where-the-concept-is-silent) has a worker that filled its free lanes set the queue's key, because a set wakes one member. With one lane, the default, every claim that takes an item fills it, so every item hands on, and some member sends a claim and a look that find nothing: about five statements per item instead of three, and the same at every end of a hold in a scoped queue. This is a statement while nothing is due (principle 5). Is the cost accepted as an exception to principle 5, or does `IWorkStore.ClaimAsync` return, with its claims, whether more claimable work remains, which changes the provider contract of Stage 1? |
| **Until answered** | The plan keeps W16 as written; no sprint of Stage 1 implements it. The question is answered before the contract of Stage 2 is approved |
| **Decides** | The owner |

### Q5. Which exception ends `RunAsync` when two of its rules meet

| | |
| --- | --- |
| **Found** | 27 November 2026, in round 3 of the review of S1 |
| **Question** | A `WaitAsync` that fails ends `RunAsync` with that exception, after the stop. During that stop a failed write may raise `Failed`, and a subscriber that throws then ends `RunAsync` with that raise's exception. Which of the two does `RunAsync` end with? |
| **Until answered** | The documentation of `WorkWorker` names both rules and not the case where they meet. S7, which implements `RunAsync`, does not start before the answer |
| **Decides** | The planner, with the contract of Stage 2 |

## Answered

### Q4. A cancellation that the SQL Server driver reports as its own error

| | |
| --- | --- |
| **Found** | 27 November 2026, in round 2 of the review of S1 |
| **Question** | `IWorkStore` says, by the cancellation rule of the [plan](epic-1-first-release.md#rules-for-every-public-member), that a cancellation that comes after the call ends the task cancelled. Microsoft.Data.SqlClient may report a command cancelled through its token as a `SqlException` instead of an `OperationCanceledException`, and W10 forbids a provider to catch a driver error in an enqueue. Can the SQL Server provider keep both rules, and if not, which one gives way? |
| **Until answered** | The contract keeps the rule as written. S3, which documents the errors of each enqueue of SQL Server and MySQL, does not start before the answer |
| **Decides** | The planner when both rules can hold, and it reports to the owner; the owner when the cancellation rule of the public contract has to give way |
| **Answered** | 27 November 2026, by the planner: both rules hold. Once its token is cancelled, the store ends the task cancelled whatever the driver reports, with the driver's exception as the inner one; that is the caller's own cancellation, not a database error, so W10 holds as written. The rule lives in S3's brief, for `SqlServerWorkStore`, and S14 tests it |

### Q8. Where the worker of `AddWorkWorker` gets its client

| | |
| --- | --- |
| **Found** | 27 November 2026, in round 2 of the review of S1R |
| **Question** | S4's `AddWork` creates the `WorkClient` with the system clock, and an application that gives its `ISignals` another clock creates the client itself. S4's brief does not say where `AddWorkWorker`'s worker gets its `WorkClient`: from the container, so that such an application registers its own client and still uses `AddWorkWorker`, or only from `AddWork`? |
| **Until answered** | S4's brief says nothing of it. S4 does not start before the answer |
| **Decides** | The planner, as it follows from the hosting contract, and reports to the owner |
| **Answered** | 27 November 2026, by the planner: the worker of `AddWorkWorker` uses the container's `WorkClient`, whether `AddWork` or the application registered it. The rule lives in S4's brief |

### Q6. Whose clock a worker uses

| | |
| --- | --- |
| **Found** | 27 November 2026, in the architect's review after S1 |
| **Question** | W6 requires the `TimeProvider` of every worker's options to be the clock of the client's `ISignals`, which exposes no clock, so the core cannot check it, and two workers of one client may differ. The planner recommends giving the clock once to the client, `WorkClient(IWorkStore store, ISignals signals, TimeProvider? timeProvider = null)`, where `null` means `TimeProvider.System`, and removing `WorkWorkerOptions.TimeProvider`; S4 would make the change in its contract commit and list it. The alternative is no change. Which one? |
| **Until answered** | The contract keeps `WorkWorkerOptions.TimeProvider`. S4, whose `AddWork` creates the client, does not start before the answer |
| **Decides** | The owner |
| **Answered** | 27 November 2026, by the owner: the clock is given once to the client, as the planner recommended. The rule lives in the epic document, under "Changes of the contract" and in the brief of S4, which makes the change |

### Q7. Limits that a column of the schema fixes

| | |
| --- | --- |
| **Found** | 27 November 2026, in the architect's review after S1 |
| **Question** | Principle 10 of `AGENTS.md` says that a limit an application can observe is an option, "never a constant in the code". The lengths of a queue name (100), a key and a scope (200) and a reason (4000) are constants, because the schema's columns fix them. The planner proposes this clause for principle 10: "…; it is never a constant in the code, unless a column of the schema fixes it — the length of a queue name, a key, a scope or a reason: such a limit is part of the schema, stated in `docs/storage.md`, and changes only with the schema's version." Is it taken, in these words or the owner's? |
| **Until answered** | The constants stay as the plan's "Rules for every public member" fix them |
| **Decides** | The owner |
| **Answered** | 27 November 2026, by the owner: the clause is taken in the planner's words. The rule lives in principle 10 of `AGENTS.md` |

