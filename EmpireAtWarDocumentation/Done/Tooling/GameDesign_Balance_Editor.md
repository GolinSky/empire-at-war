---
category: Tooling
status: done
created: 2026-10-08
updated: 2026-10-09
tags:
  - editor
  - balance
  - ui-toolkit
completed: 2026-10-09
---
# Game Design Balance Editor

## Goal

- Build a Unity Editor balance workbench using built-in UI Toolkit.
- Tune existing data, compare and edit ships within/across factions, and save/apply balance presets.
- Keep runtime gameplay logic unchanged; write only approved ScriptableObject and existing prefab data fields.

## Decision

- **Scope:** implemented the data-only features from the feasibility study.
- **Outcome (2026-10-08):** installed Editor-only UI Toolkit workbench with staged edits, editable comparisons, canonical usage/alias discovery, presets and narrow apply/restore. Inventory: 77 roster entries (36 ships, 17 squadrons), 8,907 approved fields; runtime code/data unchanged by implementation.
- **Included:** ships/squadrons, faction economy and research data, weapons/damage matrix, abilities, existing hardpoints, serialized global balance/class preferences, presets and editable comparisons.
- **Deferred:** hard-coded AI difficulty/configuration, ally/enemy-specific multipliers, runtime preset selection, new mechanics and prefab structural changes.
- **Why:** existing runtime systems already consume the approved data; no additional runtime balance service is needed.
- **Editor ownership:** new tooling and preset types stay in Editor code; runtime systems retain their current data sources.
- **Reference:** [[Done/Tooling/GameDesign_Balance_Editor - Research]]. The HTML prototype is a layout reference; this plan adds explicit shared-scope labels and direct comparison editing.

## Rules

- UI Toolkit only: `EditorWindow`, `VisualElement`, built-in fields, lists/trees and split panes. No IMGUI fallback or runtime UGUI dependencies.
- Define an explicit field allowlist. Never expose every serialized property automatically or apply whole-asset JSON/preset replacement.
- Preserve GUIDs, canonical enum IDs, component wiring, prefab nesting, transforms, visual references and unrelated serialized fields.
- Separate current asset values from a staged draft. Typing, filtering, comparing, choosing or saving a preset does not write runtime assets.
- Apply only outside Play Mode; validate and show the exact asset diff before writing. Persist/import changes without manual Editor actions.
- Use canonical asset/entry/field identity across all views; display names and array indices are not identity.
- Follow [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]] where applicable to an Editor window, and [[Architecture/PROJECT_ORGANIZATION]].

## Implementation

### Window and navigation

- Menu: `Tools/Empire At War/Game Design/Balance Editor`; embedded `BalanceEditorWindow` in the shared Editor Hub.
- Header: draft change count; preset selector, Preset actions/Draft/Tools menus, primary Review & Apply action.
- Workflows: Units, Compare, Combat, Changes. Units nests Stats, Weapons, Abilities and Hardpoints; Tools exposes Global Data and Ability catalog.
- Units: full-width Compare cards and searchable Add units picker; no left roster sidebar in any workflow. Units/Compare/Combat share the chosen unit IDs and picker filters.
- Unit details: fill remaining width; draft-aware Hull/Shields/Speed and estimated base DPS; responsive field grids grouped by category/context.
- Field Info opens a closable source/identity/usage inspector. Shared scope remains visible beside every editable field; changed fields show an indicator and original-value tooltip.
- Combat: existing shared weapon profiles and damage matrix; estimated base DPS excludes target accuracy/modifiers, arcs, abilities and movement. TTK/simulations are not implemented.
- Preserve draft, selection, pins, filters, per-view scroll offsets and panel widths; close with a draft offers save/discard/cancel.

### Units workspace — 2026-10-09

- Reuse `BalanceCompareView` and `BalanceCompareSummary`: identical cards, search/faction filtering, single/bulk addition, ordering, drag and removal.
- Each Units card has **Edit unit** → existing Stats/Weapons/Abilities/Hardpoints detail editor; **Back to units** retains cards and staged edits.
- Persist `UnitDetailsOpen`; hide the field inspector on the card overview. Selected unit and draft remain canonical across views.
- Verification: Unity compilation passed; 27 live UI checks passed, including picker actions, draft-only edits, detail navigation/persistence and measured 960/1400 px content layouts. Evidence: `Library/BalanceEditor/units-layout-check.json`.
- Console retains unrelated ISDI import errors from concurrent asset work. No Play Mode or NUnit suite run for this layout change.

### Shared scope — mandatory UI

- Every editable field identifies its owner: Unit Data, Faction Entry, Prefab Override or Shared Profile.
- Multi-faction label: **Shared across factions — Republic, Separatist, … · N units**; derive names/counts from actual references.
- A profile used by one faction still says **Shared profile — Empire · N units**. No known users → **Global profile — no current users**.
- Show labels beside editable shared fields and in the detail panel, Compare view and Changes view; tooltips alone are insufficient.
- Expand **Used by** to list every affected unit and faction, including users hidden by filters. Include squadrons, structures and ability references.
- Shared managed-reference settings need their own alias warning: e.g. **Shared settings — Laser Beam + Proton Beam**.
- Filtering changes visibility, never ownership. Do not clone profiles or create faction-specific variants automatically.

### Compare and edit

- Compare uses the full workspace; no roster sidebar or field inspector.
- **Add units** opens a searchable picker with faction filtering, unit checkboxes, Select all / Unselect all for matching results, and Add selected. Individual + Add remains available. Existing comparison units are excluded; no count limit.
- Cards wrap to available width: 330 px slots, maximum 100% for narrow panels; no column cap. Click selects one card; Ctrl+click toggles selection; selected cards have an accent outline. Ctrl+A selects all compared units; Delete removes selected units. Each card’s × button removes that unit.
- Shift+click selects the inclusive range from the last ordinary/Ctrl-click anchor in displayed order; repeated Shift+click keeps that anchor. Ctrl+Shift+click adds the range. A removed/missing anchor starts a new selection at the clicked unit.
- Selection persists through sorting and redraws. Buttons and fields retain their own shortcuts; keyboard removal restores grid focus or opens/focuses the picker when no units remain. Removing comparison cards leaves the staged draft unchanged.
- Drag the card background or ↕ handle to insert before/after another card, including across rows; insertion marker shows the drop position. Escape cancels. Dragging switches order to Manual. Buttons and fields retain their input behavior.
- Order by: Manual, Price, Height Level, Availability Level (`AvailableLevel`), Class. Numeric order is ascending and draft-aware; missing stats sort last. Ties preserve current order.
- Class order: squadrons (fighter/bomber/interceptor) → corvette → frigate → cruiser → capital → heavy capital/dreadnought (`HeavyCapital`) → structures.
- Each stat has its own bordered background. Labels and values stay adjacent; card widths remain compact with two selected units.
- Health/shield bars and numeric inputs edit the draft; shared health fields retain their visible scope label and canonical linked value.
- Movement speed, height level, build cost/time and weapon range are editable controls; base DPS stays read-only. Ships use `HeightTier` enum and `Range`; squadrons use numeric `Height` and `WeaponRange`; structures use `WeaponRange`.
- **View hardpoints (N)** replaces each card’s embedded list. Opens a 620 px wide popup, maximum 560 px high, with a scrollable list, larger names, weapon assignment controls, and non-weapon subsystem labels. Close button/Escape dismiss it.
- Popup weapon edits use the existing draft, validation, owning prefab identity and shared-source rules. The popup remains open and retains its scroll position when edits refresh the comparison. No add/remove hardpoint controls.
- Detailed editing foldout, baseline selection, comparison deltas and bulk controls were removed from Compare on user request.
- Other field editing remains in Units/Combat; Review & Apply persists staged changes.
- Missing/inapplicable values display N/A; DPS remains an AI estimate.

### Combat hardpoint grid — 2026-10-09

- Combat defaults to Hardpoints; full-width 330 px unit cards reuse Compare's visual style and searchable faction/bulk/individual Add units picker.
- Compare and Combat share pinned units, picker filters and draft assignments. Removing a card preserves staged edits.
- Each card shows faction/class, estimated base DPS, mount count and all hardpoints. Weapon assignments use existing owning-prefab controls; non-weapon subsystems retain their type labels.
- Hardpoint lists have 420 px scrollable viewports. Combat has no roster; Hardpoints also hides the field inspector. Weapons and Damage matrix remain available with field Info.
- Unity compilation/import passed. Live UI checks: **47 hardpoints / 37 weapon assignments** across four Republic ships; **960/1400/1800 px** layouts, bulk/individual additions, duplicate exclusion, draft redraw/removal and shared Compare selection.
- Edited prefab bytes remained unchanged. Report: `Library/BalanceEditor/combat-grid-check.json`. No NUnit suite or Play Mode run for this follow-up.

### Supported data adapters

- **Ship/squadron:** health, regeneration, movement/flight, range, radar, existing hangar/countermeasure parameters and compatible existing ability assignments.
- **Faction:** prices, build times, capacity, limits, unlock levels, serialized research multipliers/tier costs; shared station/buildable fields retain shared-owner labels.
- **Weapons/global:** existing profile damage/timing/flags/projectile speed; damage/accuracy/shield matrix; existing serialized global/class-preference data.
- **Abilities:** catalog timing, range, AI-use category and the current settings subtype's approved parameters. Preserve managed-reference aliases.
- **Hardpoint health:** edit `ShipData.hardPointHealth` by subsystem type, separate from individual mount controls.
- **Prefab mounts:** existing weapon assignment, yaw limits, main-battery membership, unlock level and missile-defense range/reload/chance.
- Reuse explicit component bindings to discover mount ownership. Do not introduce production component searches or infer source assets from display names.
- Assignment/type changes require compatible existing components and health data; report incompatibility instead of adding components.

### Preset and write contract

- Store versioned `BalancePreset` ScriptableObject snapshots of allowlisted values; preset metadata/types are Editor-owned and not runtime dependencies.
- Preset scope is explicit: full registered balance set or a selected subset. Loading replaces that scope in the draft; other values remain unchanged.
- Identity: asset GUID + persistent object identity where required + canonical entry/field key. Resolve current serialized paths at apply time.
- Do not persist array positions, display names, runtime hardpoint IDs or managed-reference IDs as durable target identities.
- Review groups changes by asset → field; show before/after values, shared impact, derived-data updates and conflicts with external edits.
- Preflight all targets, capture a restore snapshot, apply through `SerializedObject`/Unity prefab APIs, rebuild affected derived data, save, import and read back.
- Default ship-specific prefab writes create overrides on the owning ship prefab. Editing a shared nested source is a separate explicit shared action.
- Draft Undo/Redo and Restore Previous Apply are distinct. On partial failure, report written targets and verify rollback; do not promise automatic cross-file atomicity.

### Review fixes — 2026-10-09

- Preset compatibility uses stable type, alias, dependency and source identity; live inheritance and consumer changes stay in the draft conflict schema. Existing version-1 presets remain readable.
- Loading presets takes a fresh live conflict baseline; Apply still rejects intervening external edits.
- `BalanceDraftUsage` resolves weapon/ability consumers and aliased settings from staged assignments; unit fields, Compare, Info and Changes show the draft consumers.
- Weapon assignment choices include registered profiles only. Restored missing IDs show a field error and an explicit DPS warning; preflight blocks Apply.
- Compare includes owning prefab mount fields by canonical identity; shared-source editing remains an explicit Info action. Row labels include weapon/ability/mount context.
- Roster names shrink with ellipsis; pin width remains 24 px at 255/220 px roster widths.

### Editable cards and performance — 2026-10-09

- Picker refresh failure: unnamed ListView scroll state caused `RememberScroll` to throw. Added explicit view-data keys; result clicks add immediately and retain picker/search.
- Compact controls reuse the existing typed field editor, validation, draft, shared scope and owning mount rules.
- Added `ShipData.HeightTier` to the approved schema. Compare exposes health/shields, speed, height, cost, build time, fire range and existing weapon assignments.
- Startup: compute prefab topology once per unit, reuse for every dependent field. Measured inventory build: **10.605 s → 1.794 s**; direct window opening: **2.283 s**.
- Save: reuse Apply’s verified inventory instead of a third full rebuild; preset validation/capture reads each value once; one recovery write per refresh.
- Temporary-asset verification: full **9,135-field preset saved in 407 ms**; one-field Apply/read-back **4,273 ms**, exactly two inventory scans; previous value restored successfully.
- UI verification: immediate fifth-unit addition, retained picker/search, editable speed/cost/time/range/height/weapon type, DPS **262.3 → 259.3**, all **18** hardpoints represented, **15** weapon dropdowns.
- Nine cards wrap **4/4/1** at 924 px content width; compact controls remain inside cards. Native rendering inspected.
- User draft and live balance values preserved; temporary Sandbox assets removed. No NUnit suite or Play Mode run for this follow-up.
- Reports: `Library/BalanceEditor/{controls-check,controls-save-check,controls-profile-before,controls-profile-after}.json`.

### Comparison grid — 2026-10-09

- Replaced the earlier baseline/table/foldout design with compact cards and a searchable Add unit picker.
- Health/shield: green/blue sliders with editable current values on the right; draft commits retain validation and saved-value tooltips.
- Responsive columns, unlimited rows; 330 px slots wrap to available width with no four-column cap. Separated stat backgrounds; 11–12 px values and nearby labels.
- Follow-up verification (2026-10-09): Balance EditMode **45/45 passed** in **25.02 s**; picker filtering/bulk addition, 280/720/1080/1800 px layouts, cross-row drag, Escape cancellation, ascending draft-value sorting and class sequence. No new Play Mode run.
- Preserved Unity theme colors, existing draft, selected units and window bounds.
- Verification: Unity compiled/imported cleanly; attached UI at 924/1244/2524 px content widths with 2 and 9 units. Rows wrapped 4/4/1; card widths 210–322 px; sliders/inputs stayed inside their cards.
- Picker added a fifth unit; remove button removed its card. Numeric health and shield slider changes reached the draft; original asset values were unchanged.
- Existing comparison/layout regression cases updated for cards and slider controls. No NUnit suite or Play Mode run for this UI change.
- Native fullscreen rendering inspected; report: `Library/BalanceEditor/grid-check.json`.

### Review-fix verification — 2026-10-09

- Added 11 regression cases: local/legacy/full/subset preset reloads, draft weapon/ability consumers, missing profile IDs, owning Compare edits and roster geometry.
- Final Balance run: 34/35 passed in 133.90 s; the remaining case failed only on a Unity Pipeline timeout log. Its isolated rerun passed 1/1 in 12.27 s. All 35 cases have passing results; no assertion failures.
- Earlier run also had one Pipeline timeout-log failure from an Editor-status query; its affected case passed in the final suite. No timeout errors were suppressed in tests.
- Native UI verified at 1400 × 850 and 960 × 550 px; pins visible, profile labels explicit and owning mount controls editable. Automated roster widths: 255/220 px; pin width: 24 px.
- Imported code/USS; no new compile/import/serialization errors. Original recovery JSON restored byte-for-byte; clean `MainMenuScene`; temporary test assets removed.
- Reports: `Library/BalanceEditor/review-fix-tests.json`, `review-fix-ability-rerun.json`, `review-fix-layout.json`. Full project tests and Play Mode were not repeated for these Editor-only fixes.

### Verification — 2026-10-08

- Balance EditMode acceptance: **24/24 passed**, including same-/cross-faction comparison controls, canonical keys, hidden consumers, managed-reference aliases, nested owning/shared writes, presets, external conflicts and rollback.
- Full EditMode regression: **1,279/1,284 passed**; all 24 balance tests passed in that run. Five failures are in existing AI production, engine-hardpoint and Acclamator prefab checks.
- Regression failures: `UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(5000, Recusant)`; two `TwoEngines_EitherDestructionSlowsShipAndBothReachMinimum` cases; Acclamator team-color renderer coverage and strippable helper meshes.
- Installed window opens at its minimum size with all navigation/actions accessible; native UI controls, imported metadata and full-preset capture verified. No balance import, serialization or UI Toolkit errors.
- Play Mode: clean MainMenu → default Republic/Separatist Coruscant battle; AI production active, 8 entity views, 24 fighters and 100 weapon mounts observed. Apply blocked during Play Mode; no errors while running.
- Play Mode shutdown: existing UI route disposal raised two `BaseUi.SetParent(null)` errors through `EconomyUiController.LateDispose` / `CoreGameUiController.LateDispose`. No balance-editor stack frames; runtime teardown remains outside this data-only scope.
- Editor returned to clean `Assets/Scenes/MainMenuScene.unity`; temporary test assets removed. Reports: `Library/BalanceEditor/{balance-test-results,full-editmode-results,playmode-smoke}.json`.

### UI redesign verification — 2026-10-08

- Balance EditMode suite rerun on user request: **24/24 passed**, 0 failed/skipped; 62.12 s. Report: `Library/BalanceEditor/ui-redesign-tests.json`.
- Native layout inspection: 1400 × 850 and 960 × 550 px; roster rows 32 px, full-width details, wrapping stat cards and closable field inspector.
- Attached UI controls: draft edits, changed indicator, original-value tooltip and field error verified without live asset writes.
- Session-safe checks ran in the existing Play Mode session. Final scroll patch imported after the user exited Play Mode; temporary import pause restored automatically.
- Scroll restoration waits for virtualized list measurement; ignores unlaid-out trees when capturing positions.
- Full regression and another combat Play Mode run were not repeated for the layout change.

## Edge Cases

- Ordinary ship weapon range currently follows `ShipData.Range`; profile `WeaponProfile.Range` must not be presented as its effective fire-range control.
- Hardpoint health is per type per ship; individual mount health needs runtime support and stays outside scope.
- Weapon loadouts, squadron member counts and hull bounds are derived; do not let edits misrepresent prefab composition.
- Mount weapon changes → recompute only affected loadouts; do not call current project-wide `WeaponLoadoutBaker.BakeAll` unchanged.
- Shared profile/ability changes → show all affected consumers, including aliases inside one catalog and users outside the selected faction.
- Ability descriptions can contain stale numeric text after tuning; flag affected descriptions for review.
- Validate finite values, required positive timings, allowed ranges, canonical IDs and current subtype/field schema; missing/incompatible targets block Apply.
- External edits, renamed/missing fields, changed prefab nesting and changed alias relationships require conflict resolution; never silently skip writes.

## Files

- New Editor code: `Assets/Scripts/Editor/Balance/`; one top-level type per file.
- Preset assets: `Assets/Settings/Data/Balance/Presets/`; no runtime or Addressables registration.
- Tests: `Assets/Scripts/Tests/Editor/Balance/`.
- Core responsibilities: window/views; draft and change set; supported asset adapters/usage index; preset persistence; apply/restore.
- Existing source/data map and documentation links: [[Done/Tooling/GameDesign_Balance_Editor - Research]].

## TODO

1. [x] **Inventory and adapters**
   - Resolve current faction/unit/catalog/prefab mappings; register approved fields, canonical identities, usage and aliases.
   - Verify every current roster entry resolves and shared fields report their actual consumers.
2. [x] **Window and draft**
   - Build UI Toolkit navigation, virtualized lists, typed detail controls, validation and recoverable draft state.
   - Verify filtering/row reuse/domain reload preserve selection and draft without modifying runtime assets.
3. [x] **Editable comparison**
   - Same-/cross-faction selection, compact four-column cards and editable health/shield drafts. Detailed Compare editing, deltas and bulk UI were superseded by the 2026-10-09 grid request.
   - Verify local edits remain local; shared edits update all linked cells and report hidden consumers exactly once.
4. [x] **Abilities, global data and hardpoints**
   - Add native UI Toolkit subtype controls, shared labels, prefab ownership and conditional assignment validation.
   - Verify nested prefab overrides preserve source assets, references and structure; derived loadouts stay synchronized.
5. [x] **Presets and apply/restore**
   - Implement save/load/scope/versioning, diff preflight, external-edit conflicts, persistence and restore.
   - Verify round trips and failure recovery preserve values, aliases, asset references and unrelated fields.
6. [x] **Acceptance and regression**
   - Cover canonical identity, shared-impact discovery, compare editing, mixed-value bulk edits, prefab ownership and preset/apply recovery with meaningful EditMode tests.
   - Before running Unity tests, inspect/save named dirty scenes; untitled dirty scenes block tests. Run asynchronously and poll to completion.
   - Verify saved/imported assets, Console, and the next clean gameplay session; record actual checks and remaining limitations.
7. [x] **Complete plan lifecycle**
   - When implementation and agreed verification pass, mark done; move plan/research to `Done/Tooling/`, move the full backlog entry and repair links.
8. [x] **UI structure refinement**
   - Four workflows, compact inline-pin roster, nested unit tabs, responsive field grids, draft overview and optional inspector.
   - Wide/minimum layouts, field indicators/errors, imported scroll restoration and Balance EditMode 24/24 verified.

9. [x] **Review fixes — 2026-10-09**
   - Preserve preset compatibility after own prefab overrides; keep external-edit conflict checks.
   - Reject missing weapon profiles without breaking the panel; preview draft weapon/ability consumers.
   - Keep roster pins visible; identify comparison profiles; expose owning mount edits in Compare.
   - Add regression coverage and verify wide/minimum Unity layouts.

10. [x] **Compact comparison grid — 2026-10-09**
    - Replaced Compare roster with searchable Add unit, removed selection cap, arranged four cards per row.
    - Separated stat surfaces, reduced value sizes, retained editable health/shield bars, removed baseline/deltas and detailed editing.
    - Unity compilation, live 2/9-card geometry, add/remove and draft-only control changes verified.

11. [x] **Editable comparison and performance — 2026-10-09**
    - Immediate unit selection with retained search; editable source stats, height enum and existing hardpoint weapon types.
    - Removed repeated topology/rebuild/value-read work; retained persistence, conflict checks and rollback.
    - Live UI, narrow nine-card layout, temporary full preset save and Apply/Restore checks passed.

12. [x] **Bulk picker, responsive columns and comparison ordering — 2026-10-09**
    - Added select/unselect-all and multi-unit addition; retain faction/search filtering and exclude compared units.
    - Removed the four-column limit; verify 1/2/3/5 columns and resize back to two columns.
    - Drag handle with before/after markers and Escape cancellation; retain manual order in persisted pins.
    - Draft-aware ascending price/height/availability and requested class sequence; missing stats last.
    - Balance EditMode 45/45 passed in 25.02 s; Unity compilation/import and Console error checks clean.

13. [x] **Background dragging and hardpoint popup — 2026-10-09**
    - Card backgrounds and the existing handle drag across rows; buttons and fields keep normal input.
    - Replaced embedded hardpoint lists with View hardpoints; 620 × ≤560 px popup lists all mounts and preserves draft weapon editing.
    - Popup remains open across comparison refreshes; scroll offset is retained on its own list rebuild.
    - Balance EditMode **46/46 passed** in **27.43 s**; background drag, control exclusion, popup geometry/all-mount coverage and draft-only edits verified. Console errors: 0. No Play Mode run.

14. [x] **Comparison selection and keyboard removal — 2026-10-09**
    - Click selects one card; Ctrl+click adds/removes cards; Ctrl+A selects all compared units; Delete removes selected cards.
    - Accent outline, retained selection across sorting/redraws, keyboard focus restoration and normal field/button input verified. Removal leaves drafts unchanged and returns units to the picker.
    - Popup test uses a persistent host with the popup’s size to avoid desktop-focus dismissal during assertions.
    - Balance EditMode **49/49 passed** in **36.40 s**. No Play Mode run.

15. [x] **Shift range selection — 2026-10-09**
    - Inclusive forward/reverse Shift+click ranges follow displayed order; repeated ranges keep their anchor. Ctrl+Shift+click adds a range.
    - Ten-unit range, shrinking range, reordered cards, removed anchor and attached pointer input verified.
    - Balance EditMode **50/50 passed** in **45.12 s**; Console errors: 0. No Play Mode run.
