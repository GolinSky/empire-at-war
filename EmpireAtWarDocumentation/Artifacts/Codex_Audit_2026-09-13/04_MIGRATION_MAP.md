# Repair plan and preservation map

This plan is reviewable but unapplied. It reuses existing ownership and locations; it does not introduce a new framework, permanent role, or universal skill. A later repair request should identify the batches to apply. Obsidian configuration/credential changes must be named explicitly because the current repository protects them separately.

## Ordered repair batches

| Batch | Concrete scope | Done when | Boundary / rollback |
|---|---|---|---|
| A — Small documentation corrections | Three staged patches: root Serena recovery path; role README parent-model statement; Mac guide login/presence-check examples | Diffs match current files and help, preserve all other instructions | No runtime changes. Reverse the individual patch |
| B — Make settings sync preserve the current Mac | `SYNC/home/dot_codex/config.toml.tmpl`, rule template if approved, relevant sync ownership guide; deployed files only for intentional corrections | Read-only rendering no longer proposes unreviewed model, runtime-binding, UI, trust or permission changes | Snapshot source and target; no blanket `chezmoi apply`; preserve Windows donor behavior |
| C — Restore Mac vault access | Native project `empire-vault` declaration and the three compatibility mirrors, after actual Mac package discovery | Same intended stdio vault server reads a known note, correct root on Mac and Windows | Explicit Obsidian configuration authorization; no guessed package install, REST bridge or port/credential change |
| D — Align skill and role boundaries | Project Graphify eligibility; optional per-role permission/tool policy after intended boundary is decided | No prohibited skill routing; received child permissions/tool list match policy in an authorized fixture | No global skill deletion, no permission broadening; source + deployment change together |
| E — Reconcile advisory docs and credential handling | Targeted existing vault navigation/README text, active-tool descriptions and separate credential remediation | Canonical root policy and current integrations are clear; no literal credential in intended current docs | Explicit Obsidian credential/config scope where applicable; preserve useful history; no automatic Git history rewrite |
| F — Optional legacy cleanup | Proven no-op flags and obsolete broad execution allowances only | Version-specific recognition and intended approval behavior verified | Not required for functioning Unity/Serena; no speculative replacement flags |

Prioritize C and the exposure review in E when explicitly authorized; B must precede any synchronization. A can be handled independently. D/F are policy decisions, not prerequisites for writing this audit.

### Batch B implementation decisions

1. Re-read the source/installed key-level difference list and both versions; do not apply today's snapshot blindly after an app update.
2. Preserve this Mac's Astra/xhigh and current desktop/runtime bindings. Keep a separate Windows rendering. Do not copy a Mac absolute path into the Windows branch.
3. Establish which keys are durable shared intent, per-host preference, app-owned generated state, or local grants. The current whole-file template mixes them. Choose a supported preservation/merge mechanism or explicitly keep the app-owned host config outside whole-file synchronization before implementation; no guessed include key or wholesale template replacement.
4. Render to temporary storage and compare parsed values. Applying a rendered full file is acceptable only when every change is intentional and local app state is preserved.
5. Verify source/target mapping after the authorized write and after a second render. No commit/push is included unless requested.

### Batch C discovery before an exact patch

The Windows launcher is known, but a verified Mac mcpvault package location was not established. Therefore no executable Obsidian patch is staged. The repair must first locate a local installation or separately obtain installation authorization. Preserve the vault root and server identity, inspect startup behavior, then produce the exact platform-specific change. Saving Markdown into the vault does not require repairing this server.

## Semantic preservation map

No current instruction is retired by this audit. This table maps every existing global/root section and the other affected surfaces to a disposition; optional moves require a later per-paragraph trace before implementation.

| Source / obligation | Disposition | Destination / reason |
|---|---|---|
| Global Think Before Coding | Keep; optional wording refinement only if pauses are demonstrated | Same global file; retain meaningful uncertainty and scope handling |
| Global Simplicity First | Keep | Same file; preserve minimal scope and no speculative abstractions |
| Global Surgical Changes | Keep | Same file; protect unrelated user work |
| Global Goal-Driven Execution | Keep, subject to project test gate | Same file; concrete completion criteria remain useful |
| Global Engineering/Error Handling | Keep | Same file; project C# details specialize these rules |
| Global Context7 and documentation priority | Keep | Same file; no framework API guessing |
| Root Authority/Configuration Ownership | Keep; optionally clarify document-vs-runtime hierarchy | Same root file |
| Root Obsidian Configuration Protection | Keep verbatim | Same root file; future repair authorization must be explicit |
| Root Unity Tooling | Keep | Official `unity` CLI and Pipeline route |
| Root Serena Navigation | Keep, repair only stale fallback path | Same section; staged patch preserves no-onboarding/no-global-registration constraints |
| Root Subagent Orchestration | Keep | Independent scopes, one overlapping writer, conditional review/profiling, test runner only on request |
| Root MVP / SOLID / GRASP / Patterns | Keep | Same root; no architecture refactor |
| Root Unity Constraints/Error Handling | Keep | Same root; naming, DI, assets and fail-fast requirements preserved |
| Root Project Placement Summary | Keep routing | Existing `Architecture/PROJECT_ORGANIZATION` reference; restore MCP route separately |
| Root UI/UX Recipe Manual | Keep routing | Existing full `Rules/UI_UX_GUIDELINES` read for applicable UI tasks |
| Root Code Simplicity | Keep | Same root; no redundant validation helpers |
| Root Serialized Field Naming | Keep | Stable unprefixed serialized names |
| Root C# File Organization | Keep | One top-level type per matching script |
| Root Unity Test Safety | Keep verbatim | Explicit execution gate, dirty-scene preflight and async/polling protocol |
| Root Unity Asset Persistence | Keep | Scoped refresh/reserialize/save/verification, no user-dependent saving |
| Root Unity CLI Argument Rules | Keep | Exact parameter syntax and schema inspection |
| Root Tooling/Execution Constraints | Keep verbatim | Graphify ban and no unsolicited automated tests |
| Eight role files | Keep scope/model/effort/non-recursion | Permission/tool restrictions are a separate policy batch |
| `Rules/AGENTS.md` partial duplicate | Optional reconcile, no deletion now | Explain canonical root source without dropping scoped obligations; its omissions are not overriding negations |
| Personal Graphify skill | Scoped eligibility adjustment | Keep outside permitted project boundaries; update owned sync source |
| Vendor/system/plugin skills | Keep | No demonstrated defect justifying installed-cache edits |
| Global model and reasoning | Preserve | Astra/xhigh on Mac, not changed to match old guide |
| Project agent defaults and count | Preserve | Existing supported fields and named model choices |
| Unity/Serena MCP bindings | Preserve working bindings | Only recovery documentation/mirror routing corrected when appropriate |
| `empire-vault` | Correct platform bindings later | Same stdio integration, separately authorized |
| Disabled Graphify/legacy Unity tombstones | Keep | Verified effective project isolation |
| Rules / hooks / trust | Preserve unless a named policy repair is approved | Do not infer removal authority from cleanup scope |
| Historical vault guides | Retain useful content; label status if approved | No deletion based on age or size |

## Staged proposals

The three `.patch` files under `patches/` are plain text review artifacts. They were not applied. Each addresses only the named lines; source hashes must be rechecked before later application. There is intentionally no patch with guessed Mac Obsidian paths, broad sandbox changes, wholesale plugin deletion or replacement sync configuration.
