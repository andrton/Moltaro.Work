# Branches and releases

How the repository is branched, what `main` refuses, and how a version is released. The
steps of a sprint are in the [development process](process.md); this document says where
its commits go.

## In short

- **`main` is the only permanent branch.** It always builds and passes its tests, and
  every release is cut from it. There is no `develop` branch and no release branch.
- **All work happens on a short branch** and reaches `main` through a pull request.
  Nobody commits to `main` directly: not an agent, not the owner.
- **A release is a tag** `vX.Y.Z` on a commit of `main`. Pushing the tag publishes the
  packages.

`main` can be both the place where work is integrated and the place releases come from
because of how features are built here. A feature enters as contract: what is not
implemented throws `NotSupportedException` and its tests are skipped. A merged part of an
epic therefore never leaves `main` broken or half-working, and a sprint is merged as soon
as it closes instead of waiting for the whole epic.

## Branches

| Branch | Cut from | For | Lives | Merged by |
| --- | --- | --- | --- | --- |
| `main` | — | The integrated, green state; the source of every release | Always | — |
| `epic-<n>/sprint-<nn>-<name>` | `main` | One sprint of an epic, on the full or the refactoring track | Until the sprint closes | Pull request, squash |
| `fix/<name>`, `docs/<name>`, `chore/<name>` | `main` | The light track: a fix, a document, repository upkeep; under `chore/`, also a refactoring sprint while no epic is open | A day or two | Pull request, squash |

Examples: `epic-1/sprint-03-command-signing`, `fix/poll-timeout`,
`docs/branching`.

- **Every pull request is squashed** into one commit of `main`, whose subject names the
  sprint or the change. The commits of a sprint — contract, tests, implementation, fixes —
  are never squashed on its branch. After the merge the branch is gone and the series is
  in one place, the pull request on GitHub: that is where "tests first" can be checked. A
  clone of the repository does not carry it.
- **One sprint of an epic at a time.** The next sprint branch is cut from `main` after the
  previous one is merged. Light-track branches may exist next to it.
- **A small change that comes up during a sprint goes into the branch of that sprint**,
  not into a branch of its own: a rule, a document, a file of the repository's settings,
  a sentence of documentation. The [development process](process.md#a-sprint-on-the-full-track)
  says what makes it part of the sprint. A light-track branch is for a change made while
  no sprint is open, or one that cannot wait for the open sprint.
- **A branch that fell behind `main` is updated by merging `main` into it.** A branch with
  an open pull request is never force-pushed.
- **The branch is deleted after the merge.**

A maintenance branch `release/<major>.<minor>`, cut from a release tag to fix a released
version after `main` has moved on, is not needed yet and is not set up. It is added when
the first such fix is.

## Who does what

- **An agent** commits to the work branch of its task and stops there. It does not push,
  open a pull request or merge unless the owner tells it to.
- **The coordinator** creates the sprint branch and, when the sprint is closed, reports
  that it is ready to merge.
- **The owner** pushes the branch or has it pushed, opens the pull request and merges it.
  Merging is the owner's approval; there is no second reviewer on GitHub. The review
  itself happened in the sprint, before the pull request.
- **With the owner's standing permission**, the coordinator does the owner's part for a
  closed sprint. When the sprint is closed and no decision of the owner is open, it
  pushes the branch, opens the pull request and squashes it into `main` once every job
  that CI runs for the pull request has passed, also a job that is not a required check.
  While a decision of the owner is open it does none of the three. The permission is
  given for an epic and recorded in its document; the closed sprint under it stands for
  the owner's approval, and the coordinator reports after the merge.
- **What the permission never covers.** A merge with a failed or missing job: a failed
  job reopens the sprint, its cause is fixed and reviewed as any finding is, and only
  then is the pull request merged. A force-push. A release tag. Applying a ruleset or
  changing a setting of the repository: a sprint that changes a ruleset file is merged
  like any other, and the coordinator reports that the owner has the ruleset to apply.

## What `main` refuses

Enforced by the repository ruleset in
[`.github/rulesets/main.json`](../../.github/rulesets/main.json):

| Refused | Why |
| --- | --- |
| A direct push | Every change is seen as a pull request and checked by CI before it lands |
| A pull request whose `build` check has not passed, or whose branch is behind `main` | `main` stays green: what was tested is what gets merged |
| A pull request with unresolved review conversations | A question asked on the change is answered before it lands |
| A force-push, or deleting the branch | History of `main` is never rewritten |
| A merge commit or a rebase-merge | Every change lands as one squashed commit; the squash is the only merge method of this repository |

No approving review is required, because there is one owner. Nobody is exempt, the
repository administrator included: agents act with the owner's credentials, so an
exemption for the owner would be an exemption for every agent. In an emergency the owner
switches the ruleset off in the repository settings, and on again afterwards.

CI runs on every pull request and on every push to `main`, also for a change that touches
only documents: a required check that does not run would block the pull request.

## Releases

- **Versions** follow semantic versioning. Until a first production adopter runs on the
  library, the version is `0.x.y`, and a release is a regular version, not a pre-release.
- **A release is made by the owner**, or by an agent that the owner tells to make it, for
  that version. First a pull request sets `<Version>` in `src/Directory.Build.props`, the
  version of local builds, to the next patch with `-dev`, for example `0.1.2-dev` for
  0.1.1, so that a local build never sorts below a release. Then, with `main` green, tag
  the commit and push the tag. The [Publish](../../.github/workflows/publish.yml) workflow
  builds, tests and packs that commit with the version of the tag and pushes the packages
  to nuget.org; it refuses a tag that is not on `main`. There is no stored API key: the
  workflow signs in through nuget.org Trusted Publishing.
- **Every package is released together**, with the version of the tag: the core, the three
  providers and the hosting package.
- **A release tag is permanent.** The ruleset in
  [`.github/rulesets/release-tags.json`](../../.github/rulesets/release-tags.json) refuses
  moving or deleting a `v*` tag. A wrong release is followed by a new version, never by a
  re-tag.

```bash
git tag v0.1.0
```

```bash
git push origin v0.1.0
```

## Setting it up on GitHub

The rules are files in the repository so that they can be reviewed, but GitHub does not
read them by itself. The owner applies them: once, after the first push, and again
whenever a file changes. With the GitHub CLI signed in as the owner, from the repository
root.

The first time, each file creates its ruleset:

```bash
gh api repos/{owner}/{repo}/rulesets --input .github/rulesets/main.json
```

```bash
gh api repos/{owner}/{repo}/rulesets --input .github/rulesets/release-tags.json
```

```bash
gh api -X PATCH repos/{owner}/{repo} -F allow_merge_commit=false -F allow_squash_merge=true -F allow_rebase_merge=false -F delete_branch_on_merge=true
```

When a file has changed, the ruleset that exists is replaced, not created again. The
first command lists the rulesets with their numbers; the second replaces the one whose
number is given with the content of the file:

```bash
gh api repos/{owner}/{repo}/rulesets
```

```bash
gh api -X PUT repos/{owner}/{repo}/rulesets/RULESET_ID --input .github/rulesets/main.json
```

A setting of the repository is not part of a ruleset. When the merge methods change, the
`PATCH` command above is run again as well, so that the repository and the ruleset agree.

The same can be done by hand. A new ruleset: repository **Settings → Rules → Rulesets →
New ruleset → Import a ruleset**. A changed one: open it there and edit it. The merge
methods of the repository are under **Settings → General → Pull Requests**.

GitHub enforces rulesets in a public repository on every plan, and in a private
repository only on a paid plan. While the repository is private on a free plan, the rules
hold by this document and by [AGENTS.md](../../AGENTS.md).

**Publishing to nuget.org.** Once, before the first release, the owner signs in to
nuget.org and adds a policy named "Moltaro.Work" under **Trusted Publishing**: package
owner `andrton`, repository owner `andrton`, repository `Moltaro.Work`, workflow file
`publish.yml`, no environment, scope "Push new packages and package versions", glob
pattern `Moltaro.Work*`. The workflow then gets a short-lived key for each run; no API key
is created or stored. Renaming the workflow file or the repository breaks publishing until
the policy is changed to match.

nuget.org keeps a new policy active for seven days until a first publish fixes it to the
repository. When the first release comes later than that, the owner presses **Activate
for 7 days** on the policy before pushing the tag.

## The first commits

The repository starts with the commit GitHub made when it was created and the commit that
sets the repository up — these rules, the process, the concept agreed with the owner and
the build settings — both directly on `main`, before the rules are on. Everything after
them follows this document.
