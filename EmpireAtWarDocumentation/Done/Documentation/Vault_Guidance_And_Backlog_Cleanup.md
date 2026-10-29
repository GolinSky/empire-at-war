---
category: Documentation
status: done
created: 2026-10-29
updated: 2026-10-29
completed: 2026-10-29
---
# Vault Guidance and Backlog Cleanup

## Goal

- Shrink `TODOs/` to plans with real remaining work; move code-complete plans to `Done/`.
- Give every acceptance gate agents cannot run (playtest, listening, tests) one owner, outside the active plan list.
- Then fix the conflicting guidance and navigation from the audit (G1–G8, N1–N3, P1–P4).
- Evidence: [[Done/Documentation/Vault_Guidance_And_Backlog_Cleanup - Research]] (audit + triage, 2026-10-29).

## Problem (verified 2026-10-29)

- 80 primary plans in `TODOs/`; 38 in `Done/`. 72 are `in-progress`.
- About 40 have every implementation step checked. Their only open items are user in-game, listening, or test acceptance.
- Agents cannot close those gates: tests and Play Mode are disabled (`AGENTS.md` → Local Test Execution Override). Lifecycle step 4 requires "applicable verification is satisfied" → these plans can never move. The backlog only grows.
- Closure is inconsistent: `Done/Features/Wavecrest_Import` and `Peleset_Import` closed with "runtime balance remains unverified". 20 other import plans with the same evidence stay active.
- 10 overlapping performance plans each repeat matched-capture and Development Player gates.
- The previous version of this plan covered only G/N/P fixes. It reinforced keeping these plans active and had no triage step.

## Decision D1 — chosen A (user, 2026-10-29)

Where do user-only acceptance gates live once the implementation is complete?

- **A (recommended): close the plan and use one acceptance queue.** Move the plan to `Done/<Category>/` with `status: done` and `acceptance: pending-user`. Copy each open gate as one line into `TODOs/User_Acceptance_Queue.md`, grouped by area, with a link back. If the user finds a defect, reopen the plan or open a bug plan.
- **B: add a separate status lane.** Keep the plans in `TODOs/` but set `status: awaiting-acceptance`, and give them their own backlog section apart from Active. The folder stays big, but the Active list shrinks.
- **C: change nothing structurally.** Only correct facts and compress entries; about 75 plans stay active.
- Applied: `AGENTS.md` Plan Lifecycle step 4 sub-bullets (close with `acceptance: pending-user`, queue line per gate, `acceptance: passed` when cleared) + edge case for failed gates.

## Rules

- `AGENTS.md` owns policy; live source owns implementation facts.
- No Obsidian configuration, credential, plugin or auth edits; protected legacy setup sections untouched.
- No tests, test preparation, Play Mode, builds, gameplay or asset changes.
- Note writes/moves through `empire-vault`; on 2026-10-29 it was disconnected → Phase 1 moves used `git mv` + scripted link rewrite, verified by a vault-wide wikilink scan. Preserve unrelated user edits.
- Close a plan only when every implementation step is checked **and** live source/git confirms it. A checkbox alone is not evidence.
- A plan with an unimplemented feature (missing ability, unimplemented phase) stays active, whatever its acceptance state.
- Never delete plans or companions; move them with research/mapping/visual companions; repair inbound links.

## Implementation

### Phase 0 — Policy (after D1)

1. [x] Record D1 here. If A/B: patch `AGENTS.md` Plan Lifecycle (acceptance field/status, queue note, reopen rule) with user-approved wording.
2. [x] If A: create `TODOs/User_Acceptance_Queue.md` (sections: Imported Units, Audio, UI, Gameplay/Balance, Performance, Tests-on-request). Index it in [[TODOs/TODOs]].

### Phase 1 — Triage active plans

3. [x] Classify every primary plan using the triage table in the research note; verify each candidate against source/git before moving.
   - **Close** (implementation complete, only user/test gates) → queue + `Done/`.
   - **Active** (real implementation remaining) → keep; next gate in one line.
   - **Merge** (overlapping ownership) → one owner; the other is marked superseded and moved to `Done/` with its disposition.
   - **Stale** (no change since ≥2026-10-05 and unclear) → ask the user: continue, cancel, or close.
4. [x] Process in batches of ≤10 per category: move plan + companions, set frontmatter (`status`, `completed: 2026-10-29` or actual date, `acceptance`), move the backlog entry to `Done`, add queue lines.
   - 2026-10-29: Close class done — 38 plans (+8 companions) → `Done/`, `acceptance: pending-user`; 38 backlog entries moved to Done; gates in [[TODOs/User_Acceptance_Queue]]. Kept active despite candidacy: Defense_Satellites_Plan + Review_Fixes (uncommitted satellite prefabs), Hardpoint_Health_From_Hull_Plan (edited 2026-10-29), Settings_Implementation_Plan (unbuilt settings rows in TODO). Active primary plans 80 → 42. Merge/Stale classes (steps 5–7, stale list) remain.
5. [x] Performance consolidation (P2 extended): pick one owner for measurement/acceptance. Proposed: `Battle_CPU_Performance_Plan` for CPU, `Battle_FPS_Recovery_Plan` for rendering. Fold the shared "matched captures + Development Player" gates into that owner. Remaining plans keep only their unique implementation items, or close.
   - 2026-10-29: CPU owner absorbed CPU Fixes (23 open items), Attack (8), Burst Jobs (11); rendering owner absorbed Rendering Audit (6), Particle Rendering (10), Projectile VFX (20). Sources → `Done/Optimization/` with `disposition: merged`, `merged_into`. UI_Canvas_Routing and Loading_Freeze stay separate (distinct scope).
6. [x] Hazards (P1): original → gameplay/events; Shape Rework → geometry/visual/perf; Rework → close as superseded once its open items (decor ring, final) are transferred.
   - 2026-10-29: single owner = [[TODOs/Features/Space_Hazards_Plan]]. Rework + Shape Rework merged and closed; IonStorm data refactor and SDF bake timing transferred; decor ring frame time + sheet overdraw → FPS Recovery; tests/user review → queue.
7. [x] Wave tracker: close `Audit_Remediation_Coordination` once Wave 1/2 plans are each closed or explicitly active.
   - 2026-10-29: closed by user decision; Wave 2 ordering rule copied into Type_File_Split and Injection_And_Guard plans.
8. [x] Verify: each open obligation has exactly one owner; no plan in `TODOs/` has all steps checked; backlog Active count recorded before/after.
   - Stale plans Ship_Movement_Simplification, Ship_Abilities, SkirmishOrchestrator closed by user decision 2026-10-29 (`disposition: closed-by-user`; unfinished scope in banners).
   - Result: primary active plans 80 → 30; vault-wide wikilink scan clean (excluding pre-existing escaped table links).

### Phase 2 — Guidance fixes (audit G1–G8)

9. [x] PureModel → `Model` in the UI playbook + research; align BaseUi contract and event-subscription ownership across UI guides (G1, G2, G7).
   - G7 user rule: view subscribes to model observer events by default; controller/service only when the reaction exceeds view scope. Written in UI_CODE_BUILD_GUIDE; playbook and both AGENTS MVP lines link/align.
10. [x] Replace unconditional test/scene-prep steps in UI UX checklist and three station guides with a root-policy link (G3).
11. [x] Shrink [[Rules/AGENTS]] to an index; repoint [[Rules/UNITY_JOBS_AND_THREADING_RULES]] to root policy (G4).
12. [x] Fix stale Settings facts: `IsDirty => !Draft.Matches(Saved)` (`SettingsService.cs:23`), `SettingsRouteUiController`, countdown already extracted (G5). Settings_Implementation_Plan stays active (unbuilt rows).
13. [x] Palette (G6): user chose live UI values → rgba rewritten from prefab-verified hex (`#080C14` 0.96, `#0F172A` 0.85, `#182232` 0.95, `#141E2C` 0.95; ~20 prefabs use them). Diagram Entities label fixed (G8).

### Phase 3 — Navigation (N1–N3)

14. [x] N1: `audio-no-mvp-service-owns-decisions` is a Claude memory slug, not a vault note → convert to plain text in [[Done/Features/Faction_Interface_And_Ability_Audio_Plan]] (and audit notes).
15. [x] N2: add the 6 missing entries to [[AssetPipeline/00_AssetPipeline]].
16. [x] N3: link the 25 import records to their plan (`TODOs/` or `Done/` path after Phase 1) or mark them provenance-only.
   - Result: banner `> Plan:` in all 25 — 1 plan link (Raider Corvette), 9 no-plan records with open acceptance → queue section, 15 provenance-only. N2 index rebuilt from folder: 53 records.

### Phase 4 — Backlog compression and close

17. [x] Each Active entry: plan link, status, one concrete next gate (P4). Long histories stay in plans.
18. [x] Validate: wikilinks to moved paths resolve; old paths absent; no active duplicates; protected config sections unchanged. Record counts.
19. [x] Close this plan: `done`, `completed`, move plan + research to `Done/Documentation/`, move backlog entry, repair links.

## Outcome

- 2026-10-29: active primary plans 80 → 30; 38 closed to queue, 9 merged into 3 owners, 4 closed by user; guidance G1–G8 and navigation N1–N3 fixed; backlog entries compressed (P4).
- `empire-vault` MCP disconnected → moves via `git mv`, edits via scripts; vault-wide wikilink scan clean. No tests, Play Mode, builds or asset changes. Obsidian configuration untouched.

## Edge Cases

- Plan checked but source disagrees → keep active, record the missing piece.
- Import with a pending ability (ARC-170 S-Foils, Mandator Tractor Beam, V-19 Hunt, Malevolence Vulture launches, NTB-630 textures) → active, or split: close the import and open a narrow ability task. Decide per unit.
- User acceptance fails later → reopen the plan per lifecycle edge case, or open a bug plan; remove the queue line.
- Plan blocked on user approval (ShipLit gallery) → queue line + close, or keep `blocked` — follows D1.
- Uncommitted WIP referenced by a plan (Original_Values_Rebalance) → stays active.

## Files

- [[Done/Documentation/Vault_Guidance_And_Backlog_Cleanup - Research]]
- [[TODOs/TODOs]]
- [[Rules/DOCUMENTATION_ORGANIZATION]]
- Root `AGENTS.md` → Obsidian Plan Lifecycle
