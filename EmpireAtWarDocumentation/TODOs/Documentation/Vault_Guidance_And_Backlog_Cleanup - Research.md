---
category: Documentation
status: todo
type: audit-report
created: 2026-10-29
verified: 2026-10-29
verification: vault-content-and-targeted-source-inspection
---
# Vault Cleanup Audit 2026-10-29

## Goal

- Audit guidance, instructions, artifacts and TODO ownership; distinguish duplicates from useful history.
- Cleanup execution plan: [[TODOs/Documentation/Vault_Guidance_And_Backlog_Cleanup]].

## Important Values

- Baseline: 249 Markdown notes read through empire-vault; 253 visible files, including 4 non-Markdown files.
- Areas: Architecture 6; Rules 5; Tooling 4; Artifacts 19; AssetPipeline 59; GameDesign 18; TODOs 98; Done 38; root 2.
- 75 primary active plan notes; all 75 linked directly from [[TODOs/TODOs]]. Research, mapping and visual companions excluded from primary count.
- 0 identical note bodies after newline normalization and outer-whitespace trimming. This does not rule out repeated paragraphs or overlapping purposes.
- 1,180 wikilink occurrences scanned: 1 unresolved note target; 0 ambiguous note targets. Heading/block fragments, Markdown links and external repository paths were not fully validated.
- 6 import records missing from their area index; 25 of 53 import records lack an explicit TODOs/Done wikilink.
- Static documentation audit plus targeted live-source checks. No tests, builds, Play Mode, asset changes or Obsidian configuration changes.
- Graphify documentation query returned no matching nodes; findings rely on vault contents, root policy and targeted live-source inspection.

## Decision

- Main problem: conflicting current guidance and repeated ownership/status summaries, not identical files.
- Keep repository `AGENTS.md` authoritative. Reduce [[Rules/AGENTS]] to a navigation summary rather than another maintained policy body.
- Preserve historical audits, provenance and dated verification. Never close active acceptance gates from age, checked implementation steps or an old passing result.
- No existing note deleted, moved or rewritten by this audit; only this report, its cleanup plan and the new backlog entry are authored.

## Implementation

### Confirmed guidance defects

| ID | Priority | Evidence | Proposed correction |
| --- | --- | --- | --- |
| G1 | High | [[Architecture/UI_REFACTORING_PLAYBOOK]] and [[Architecture/UI_REFACTORING_PLAYBOOK - Research]] prescribe `FeatureModel : PureModel`. Live `Assets/Scripts/BaseSystem/Model.cs` defines plain `Model`; `ReinforcementModel` inherits `Model`; Serena found no `PureModel` symbol. | Replace obsolete current examples with verified `Model` usage; preserve any old example only as explicitly historical. |
| G2 | High | [[Rules/UI_UX_GUIDELINES]] permits `BaseUi<TModel,TCommand>`; [[Rules/UI_CODE_BUILD_GUIDE]] and the playbook require non-generic `BaseUi` and explicit setup. | Align current UI guidance around the build guide and live BaseUi contract. Do not merge visual/UX guidance into architecture. |
| G3 | High | UI UX checklist asks whether all EditMode tests pass. [[AssetPipeline/Guides/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]], [[AssetPipeline/Guides/REPUBLIC_SPACE_STATION_MODEL_IMPORT_GUIDE]] and [[AssetPipeline/Guides/SPACE_STATION_MODEL_IMPORT_GUIDE]] instruct test execution and scene preparation. Root AGENTS disables tests and test preparation absent an explicit user request. | Replace unconditional operational steps with a root-policy link and conditional verification gate. Retain historical test results unchanged. |
| G4 | Medium | [[Rules/AGENTS]] already says it is advisory but repeats substantial root policy. | Keep a short authority/entrypoint note; link specialized guides. Do not create another copied rule set. |
| G5 | High | [[TODOs/Features/Settings_Implementation_Plan]] says `IsDirty = JSON compare` and lists countdown extraction as pending. [[TODOs/Features/Settings_Audit_Followups]] records both fixes. Live `SettingsService.cs:23` uses `!Draft.Matches(Saved)`; `SettingsRouteUiController.UpdateDisplayCountdown` delegates to `DisplayConfirmationCountdown`. | Correct stale implementation facts and the old `SettingsRouteController` name. Keep unverified settings acceptance open; broader controller simplification is separate. |
| G6 | Medium | [[Rules/UI_UX_GUIDELINES]] pairs `#080C14` with RGB `(3,5,8)`, although its direct 8-bit RGB representation is `(8,12,20)`; three adjacent palette rows also disagree. | Check intended styling/color-space interpretation before choosing canonical values; do not silently change the palette. |
| G7 | Medium | [[Rules/UI_CODE_BUILD_GUIDE]] says subscribe to controls and model events in view-lifecycle guidance; the playbook assigns model subscriptions to controllers and control listeners to views. | Clarify ownership once in the build guide; migration guide links that contract. |
| G8 | Low | [[Architecture/PROJECT_ORGANIZATION]] diagram describes Entities as pure C# state/business logic, while its prose permits feature UI triplets. | Align the diagram with the documented colocation rules; no asset moves. |

### Overlapping plans and stale summaries

| ID | Priority | Evidence | Proposed ownership |
| --- | --- | --- | --- |
| P1 | High | [[TODOs/Features/Space_Hazards_Plan]], [[TODOs/Features/Space_Hazards_Rework_Plan]] and [[TODOs/Features/Space_Hazards_Shape_Rework_Plan]] are all active. The original points to tapered bands; the latest records SDF/lobe shapes and shared geometry. Acceptance/performance gates recur across plans. | Original owns gameplay/deferred events; latest shape plan owns geometry/visual acceptance. Mark replaced geometry sections superseded and assign every remaining gate once. Preserve intermediate evidence; do not mark the entire feature done. |
| P2 | Medium | [[TODOs/Optimization/Battle_CPU_Performance_Plan]] still queues allocation-stack attribution; [[TODOs/Optimization/Battle_CPU_Fixes_Plan]] records 40 frames, 3,063,858 B and 45,927 resolved samples, yet also retains earlier unknown-attribution wording. | Performance plan owns measurement/acceptance; fixes plan owns implementation. Date baseline snapshots and link completed evidence. Keep unmatched performance, unreproduced spikes and parity gates open. |
| P3 | Medium | [[TODOs/Refactoring/Injection_And_Guard_Conventions]] assumes Zenject-specific injection decisions; [[TODOs/Refactoring/Zenject_To_VContainer_Migration]] records cutover progress and explicit dependency cleanup. | Re-audit CO5/CO8 against current source before execution; retire replaced instructions only, retain any surviving guard/binding work. Current closure is not established. |
| P4 | Medium | [[TODOs/TODOs]] repeats long audio/hazard implementation and test histories while plans also own that evidence. Generic “see linked plan” next gates coexist with detailed histories. | Index entries: owner plan, current status, concrete next gate. Keep full outcomes and test evidence in plans/research. Preserve nested backlog history when moving completed entries. |

### Navigation and ownership defects

- **N1 — broken wikilink:** [[TODOs/Features/Faction_Interface_And_Ability_Audio_Plan]] references `audio-no-mvp-service-owns-decisions`, which has no matching note.
  - Action: identify the intended decision source; link it if present, otherwise retain the decision as plain text. Do not invent a note merely to satisfy the link.
- **N2 — incomplete asset index:** [[AssetPipeline/00_AssetPipeline]] omits the six records listed below.
- **N3 — missing plan ownership:** 25 import records have no explicit TODOs/Done link. Absence is not proof that work is unfinished; some records describe open acceptance while others may cover completed scope.
  - Example: [[AssetPipeline/Imports/Raider Corvette Import]] lacks a direct link to existing [[Done/Features/RaiderCorvette_Import]].
  - Action: map each record to an existing plan where possible; otherwise state “historical provenance only” or create a narrowly scoped acceptance item after verifying the remaining obligation.

### Protected and intentional history

- [[00_Home]], [[Artifacts/Implementation_Plan]] and [[Artifacts/Walkthrough]] retain retired REST setup material and credential text. They already identify it as historical.
  - Do not copy credential values into the report or cleanup notes. Editing/removing that configuration material requires a separate explicit Obsidian configuration request under root policy; excluded from this plan.
- [[Artifacts/History/Graphify_Guide_Before_2026-10-20]] and [[AssetPipeline/History/ISD I Import - AOTR History]] are correctly retained superseded records.
- [[Artifacts/History/Backlog_Before_2026-10-20]] is an intentional snapshot, not a competing active backlog.
- [[Done/Refactoring/Project_Organization_Remediation_Plan]] has unchecked optional follow-up/test items; they are not evidence that the completed migration should reopen. [[TODOs/Refactoring/Project_Organization_Followup]] owns later work.
- [[AssetPipeline/Imports/Y-Wing Import]] and [[AssetPipeline/Imports/Y-Wing Bomber Import]] cover different units: Republic BTL-B ID 3 versus Rebel BTL-A4 ID 301.
- Victory, Victory I and Victory II import records cover distinct IDs 200, 202 and 203. Similar names do not justify merging.
- Plan/research pairs serve different purposes. No wholesale merge or deletion is recommended.

## Files

### Missing asset index entries

- [[AssetPipeline/Imports/CR70 Corvette Import]]
- [[AssetPipeline/Imports/Carrack Import]]
- [[AssetPipeline/Imports/Dreadnaught Import]]
- [[AssetPipeline/Imports/Invincible Import]]
- [[AssetPipeline/Imports/Pelta Import]]
- [[AssetPipeline/Imports/Recalcitrant Import]]

### Import records requiring ownership review

- [[AssetPipeline/Imports/AOTR Golan III Defense Platform Import]]
- [[AssetPipeline/Imports/Acclamator Assault Ship Imperial Import]]
- [[AssetPipeline/Imports/Assault Frigate Mk II Import]]
- [[AssetPipeline/Imports/Bothan Assault Cruiser Import]]
- [[AssetPipeline/Imports/CD-273 Corellian Destroyer Import]]
- [[AssetPipeline/Imports/CR70 Corvette Import]]
- [[AssetPipeline/Imports/Carrack Import]]
- [[AssetPipeline/Imports/Corellian Battlecruiser Import]]
- [[AssetPipeline/Imports/Corellian Corvette Import]]
- [[AssetPipeline/Imports/Dauntless Import]]
- [[AssetPipeline/Imports/Dreadnaught Import]]
- [[AssetPipeline/Imports/Droid Bomber Import]]
- [[AssetPipeline/Imports/Endurance Fleet Carrier Import]]
- [[AssetPipeline/Imports/Executor Model Import]]
- [[AssetPipeline/Imports/Home One Import]]
- [[AssetPipeline/Imports/ISD III Import]]
- [[AssetPipeline/Imports/Liberator Import]]
- [[AssetPipeline/Imports/MC50 Medium Cruiser Import]]
- [[AssetPipeline/Imports/Mon Calamari Cruiser Import]]
- [[AssetPipeline/Imports/Nebula Star Destroyer Import]]
- [[AssetPipeline/Imports/Nebulon-B Import]]
- [[AssetPipeline/Imports/Pelta Import]]
- [[AssetPipeline/Imports/Quasar Fire Import]]
- [[AssetPipeline/Imports/Raider Corvette Import]]
- [[AssetPipeline/Imports/Recalcitrant Import]]

## TODO

- Execute G1–G8 first; then N1–N3 and P1–P4.
- Repair [[Rules/UNITY_JOBS_AND_THREADING_RULES]] references to missing constructor/test sections in the partial [[Rules/AGENTS]] summary; point to root policy.
- Optional later compression: separate dated integration evidence from the reusable station guide procedure, preserving provenance and exact verification.
- Validate current-source claims before changing plan status or replacing technical examples.
- Keep protected configuration material and historical evidence intact.
