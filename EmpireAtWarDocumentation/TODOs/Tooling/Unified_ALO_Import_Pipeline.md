---
category: Tooling
status: in-progress
created: 2026-10-28
updated: 2026-10-28
tags:
  - alo
  - asset-pipeline
  - automation
  - concurrency
---
# Unified ALO Import Pipeline

## Goal

- Replace repeated per-model Python/C# generation with one versioned pipeline and declarative recipes.
- Cover source audit → Blender conversion → Unity integration → registrations → persisted verification and review images.
- Allow multiple agents to submit imports concurrently without shared scenes, lost registry entries, mixed evidence, or overlapping writes.

## Decision

- Status: implementation in progress; user authorized execution on 2026-10-28. Migration gates remain required.
- Chosen: Python coordinator/converter, focused C# Editor builders, JSON recipes, per-job evidence.
- Why: existing imports repeat preparation/conversion/build/registration; some generate source by string replacement.
- Avoid: rewriting an importer from scratch, executable recipe hooks, a generic plugin framework, bulk migration of every old import.
- Parallelism: independent source/Blender jobs; one coordinated Unity operation at a time per physical checkout.
- v1 concurrency scope: cooperating agents on one machine/local checkout. Multiple computers sharing a checkout require a different coordinator and are unsupported.
- Initial integration targets: ships and leveled stations. Reserve squadron recipe kind, but reject it until a separately tested adapter exists.
- Existing abilities and weapon profiles are references. New combat logic, ALA conversion, particles and EaW animated shader reconstruction are separate work.
- Review evidence and reuse candidates: [[TODOs/Tooling/Unified_ALO_Import_Pipeline - Research]].

## Rules

- Current user instructions and repository AGENTS.md override historical guide instructions and recorded balance values.
- Preserve source files, existing GUIDs, Addressables group structure, unrelated changes and the Git index.
- Read [[Architecture/PROJECT_ORGANIZATION]] before asset-folder work; read UI guidelines/build guide before relevant UI implementation.
- Inspect current APIs through Context7/local documentation and official unity command schemas when implementing; commands below are proposed contracts, not installed commands.
- Do not infer faction from source XML labels; RaW Republic/CIS use misleading Empire/Rebel labels.
- No silent substitute textures, fighter types, root repairs, material assignments or balance values.
- Static/Editor verification, visual review and runtime acceptance are distinct results. Never mark unrun checks passed.

## Implementation

### Current execution — 2026-10-28

- Implemented strict shared schema, durable job/queue/OS-lock ownership, input/output hashes, scoped registry-entry baselines, publication preimages/checkpoints and bounded no-write recovery.
- Implemented source binary/XML audits, explicit texture/definition overrides, numeric assembly manifests, owned Blender processes and source/FBX fidelity checks.
- Implemented `existing-assets-v1` ship-update preflight, candidate identity checks, explicit binding updates and registration merge; `apply` remains disabled pending live Editor/candidate acceptance.
- Current Rothana recipe: 22 hardpoints / 19 weapons; measured saved size `121.306477 × 43.987835 × 238.766746` project units. Preserve current tuning and root transform.
- Source triangle discrepancy resolved: `Col` loses 2 triangles and `Shadow` loses 1,498 in legacy FBX; all 11 other mesh totals match original ALO. Source-derived flat helper shading preserves authored normal data without widening tolerances.
- Durable state uses `Tools/AloImport/.state/`; Unity `Temp` evidence disappeared during work and cannot hold restart-safe ownership/preimages. `layout.json` is shared by Python/C#.
- Non-test verification: pipeline C# compiled with Unity's bundled compiler; current recipe/schema validation and Python syntax inspection passed. Tests/test preparation disabled by latest user instruction and AGENTS override; earlier test results are historical only.
- Remaining: live Editor import/candidate parity, production Rothana conversion/publication acceptance, complete Endurance/Rebel recipes and adapters, create/render/derivative capabilities, full concurrent publication/recovery acceptance and measured workflow comparison.

### 1. File ownership and placement

| Location | Purpose |
|---|---|
| Tools/AloImport/alo_import.py | Single CLI entrypoint |
| Tools/AloImport/ | Focused Python modules, schema, tests and short agent README |
| Tools/AloImport/Recipes/<Model>.json | Reviewed model decisions; checked in |
| Assets/Scripts/Editor/AloImport/ | Focused Editor builders/validators; one type per file |
| Assets/Scripts/Tests/Editor/AloImport/ | Meaningful integration/regression tests |
| Tools/AloImport/.state/Jobs/<jobId>/ | Ignored durable input snapshots, conversion staging, logs, preimages and journals |
| Tools/AloImport/.state/Coordinator/ | Durable queue/ownership metadata; actual exclusion uses OS locks |
| Tools/AloImport/layout.json | Shared Python/C# state and candidate-import layout |
| Tools/AloImport/Evidence/<Model>/<runId>/ | Curated small acceptance reports when intentionally retained |

- Proposed folders must be checked against live placement rules before creation. No asset folders are created by this planning task.
- Job IDs are generated UUIDs; model names alone never identify scratch directories, request files or reports.
- Model output paths are derived once by the layout resolver; existing migrated assets retain their verified paths and GUIDs.
- No pipeline writes to Assets during Python conversion. All publication/import happens under Unity coordination.
- Recipes contain no arbitrary Python/C#, shell commands, or source-template replacement fragments.

### 2. Recipe schema v1

- Implement a machine-readable JSON Schema plus semantic validation in Python and C#. Unknown fields and unsupported schema versions fail.
- Recipe is portable: source roots are aliases resolved by a local invocation config outside the recipe; absolute machine paths stay in job evidence.
- References to existing Unity assets use GUID as canonical identity; resolve paths for diagnostics. New assets use planned project-relative paths until GUID creation.
- Imported source object identity = piece ID + original numeric bone/mesh index. Names are descriptive audit data; duplicate names never become ambiguous lookup keys.
- A new unit's explicit enum name/value is unavoidable code registration; validate uniqueness and reserve it under coordination. Never renumber existing members.

| Field | Type / contract |
|---|---|
| schemaVersion | Integer; exactly 1 initially |
| modelId | Stable nonempty identifier; filesystem-safe; unique within recipe catalog |
| kind | ship or station; squadron rejected as unsupported in v1 |
| operation | create or update; update requires expected existing unit identity and output GUID baseline |
| source | Root alias, relative ALO/XML paths, chosen XML unit/variant, optional archive paths; resolve case-insensitively, fail on ambiguity |
| pieces | Nonempty array: unique ID, ALO relative path, purpose (hull/attachment/damaged/level); one hull for ship, one hull per station level |
| attachments | Piece instance ID, source piece, parent piece, source bone index; repeated instances explicitly listed |
| identity | Faction and existing unit identity, or proposed explicit new enum name/value; source names never choose routing |
| integration | Compatible donor/template GUID, required component/binding profile; explicit retained shared dependencies |
| dimensions | Positive target length for ship or common station size rule; orientation, pivot rule, banking angle, fitting margins with units |
| materials | Source shader/parameter-signature selectors → supported opaque/additive/cutout mapping, texture semantic, normal convention, livery policy |
| hardpoints | Unique IDs, kind/profile GUIDs, piece+bone indices or explicit authored local coordinates, arcs/muzzles, health and targeting |
| hangar | Existing squadron references, launch anchors, capacities, reserves, delays; empty if absent |
| gameplay | Existing data baseline or explicit requested values; abilities/weapon profiles referenced, not invented |
| station | Required only for station: ordered levels, common pivot/alignment anchor, mounts by level/unlock, existing shared gameplay-data GUIDs |
| derivatives | Placement, wreck source selection, icon framing/resolution, helper policy, shield fitting policy |
| decisions | Keyed records identifying selected project values, provenance and unresolved choices |
| exceptions | Narrow rule ID + affected source IDs + reason + evidence; approval reference where user approval is required |
| verification | Named tolerance profile, any justified overrides with units; required renders/check groups |

- Required fields depend on kind; forbid station fields on ships. Reject duplicate mount IDs, unsupported material policies, missing referenced pieces and invalid level ranges.
- Explicit null means an intentional supported absence, such as embedded station weapon art; missing mandatory fields fail.
- Defaults may cover mechanical export settings; gameplay, faction, target size and livery choice have no guessed defaults.
- All weapon hardpoints targetable by default; exceptions require recorded user authorization. A historical hull-only import is not general permission.
- Separate source facts from project choices: SourceAudit.json stores hashes, original matrices, materials, mesh/slot counts, texture sizes and XML provenance.
- Material selectors must resolve exactly; source signatures include shader plus full parameters, avoiding texture-name collisions.

Illustrative fragment; not a complete runnable recipe:

```json
{
  "schemaVersion": 1,
  "modelId": "Rothana",
  "kind": "ship",
  "operation": "update",
  "source": {
    "root": "rothana-original",
    "alo": "Models/Rothana_Stardestroyer_Full_Armed.ALO"
  },
  "pieces": [
    {"id": "hull", "alo": "Models/Rothana_Stardestroyer_Full_Armed.ALO", "purpose": "hull"}
  ],
  "attachments": [],
  "verification": {"profile": "static-source-fidelity-v1"}
}
```

- Real recipes must supply identity, current output GUIDs, bindings, dimensions, material and gameplay decisions. Never populate them solely from historical examples.

### 3. Command contracts

Common invocation: `python Tools/AloImport/alo_import.py <command> ... --json`.

| Command | Inputs / outputs / side effects |
|---|---|
| doctor | Read-only tool/version, source-root and coordinator checks; Unity probe joins the Editor queue |
| inspect --recipe PATH | Audit source into a new job; no project asset writes; return jobId, missing inputs and ambiguity report |
| plan --job ID | Validate frozen recipe + audit, resolve outputs/dependencies, produce hashed BuildPlan.json with read/write sets and registry keys |
| convert --job ID | Convert into owned staging directory; verify source → Blender → FBX before succeeding |
| apply --job ID --plan-hash HASH | Acquire ownership/Editor access, revalidate plan, publish/build/register/save/reload; stale hash or changed inputs fails |
| verify --job ID | Check persisted assets and source evidence; Unity-dependent reads are queued; does not silently rebuild |
| render --job ID | Queue isolated preview scenes; create model, placement, wreck and owned-color evidence; restore global state |
| status --job ID | Read coordinator snapshot; no Editor invocation; compact active/waiting/failure summary |
| resume --job ID | Reconcile journal; rerun invalid stages only; never blindly replay unknown Unity operations |
| cancel --job ID | Request cancellation; queued job cancels immediately, active mutation stops only at a safe checkpoint |
| recover --job ID | Diagnose/reconcile interrupted publication after writer quiescence; report conflicts rather than overwrite newer changes |

- Agent workflow: inspect → resolve recipe questions → plan → convert → apply → verify → render → review. Recipe change creates a new immutable job snapshot.
- `--wait-seconds N` controls caller waiting, not mutation ownership. Default 0 returns queued/running; caller polls status.
- Python invokes official unity CLI bridge with structured argument serialization; inspect registered schemas before implementation. No legacy Unity MCP server.
- Explicit verification/test requests have separate queued execution; conversion/apply never implicitly enter Play Mode.
- Idempotency key: job ID + stage + frozen input hash. Duplicate submissions return the same operation record.
- stdout: one JSON envelope; verbose logs on disk/stderr. Never stream full mesh arrays into agent context.

Envelope fields: `schemaVersion, jobId, operationId, stage, state, code, retryable, summary, counts, evidencePaths, nextAction`.

- States: queued, running, succeeded, failed, cancelled, interrupted, needs-recovery. Check states separately: passed, failed, not-run, needs-review.
- Exit codes: 0 accepted/succeeded/status returned; 2 invalid recipe; 3 ownership/input conflict; 4 unavailable tool/input; 5 verification failure; 6 interrupted/unknown mutation outcome.
- Machine error codes include SOURCE_AMBIGUOUS, OUTPUT_CONFLICT, INPUT_CHANGED, UNSUPPORTED_CAPABILITY, EDITOR_BUSY, OPERATION_UNRESOLVED and RECOVERY_CONFLICT.
- Accepted/queued is never presented as success. Preserve nested Unity result failures even when the CLI exits 0.

### 4. Reusable components

| Component | Reuse / responsibility |
|---|---|
| SourceResolver + AloAudit | Extract existing binary/XML/archive readers; preserve original indices, matrices, shaders, triangle/UV data and hashes |
| RecipeValidator + LayoutResolver | Validate decisions and produce one canonical read/write/dependency plan |
| BlenderSession | Reuse launcher/MCP transport; own process/session identity, request chunking and safe-mode limits |
| StaticConverter | Verified root/hierarchy/material repairs, constraint-to-parent conversion, source metadata, texture decode and FBX round trip |
| FidelityVerifier | Original-source comparisons, not only FBX-to-Blender comparisons; explicit helper/degenerate-face exclusions |
| UnityArtBuilder | Import settings, external material remaps, visible geometry, attachments, stable GUID updates |
| ShipIntegrationBuilder | Rebind template components, hardpoints/health/fog/team/selection/audio/hangar, fitted volumes |
| StationIntegrationBuilder | Reuse shared station-level workflow; common pivot, typed mounts, level-specific bounds/shields |
| DerivativeBuilder | Scoped preview, wreck and icon operations; preserve helper visibility and livery |
| RegistrationWriter | Upsert exact typed identities in current registries/Addressables; preserve unrelated entries |
| SavedAssetVerifier | Reload assets; inspect bindings, injection, references, dimensions, IDs and dependency closure |
| JobCoordinator + Journal | Ownership, Unity queue, frozen inputs, stage evidence and interrupted-write recovery |

- Reuse ShieldHullBaker, ShipHullHeightBaker, ShipWreckBuilder, UnitHelperMeshStripper and ShipHardPointValidator after reviewing current APIs.
- Add scoped per-unit icon generation; never invoke fleet-wide regeneration for one model.
- Keep helpers focused; do not duplicate runtime systems or add a generic extensibility framework.
- Registration targets include unit enum/data, AssetMappingData, current faction roster, existing Addressables groups, placement AssetReference GUID, UI/tooltip/icon maps, wreck and supported abilities.
- Source code additions to enums/icon mappings also require checkout mutation coordination and compilation completion before building.
- Explicit binding lists come from the template contract/generated asset map; do not reproduce legacy implicit component searches without resolving current repository rules.

### 5. Concurrent agents and resource ownership

- Canonical checkout identity resolves case, junctions and symlinks to the physical project. All tools sharing its Editor use one coordinator namespace.
- Each job freezes recipe, tool fingerprint and source hashes; agents never edit another job's scratch files or evidence.
- Same model: one active apply owner. Different models with intersecting canonical output paths or registration identities fail OUTPUT_CONFLICT before mutation.
- Reserve output paths, new unit IDs and registry keys atomically under a short coordinator metadata lock. Include parent/child directory overlap, individual assets and .meta files; acquire claims in canonical sorted order.
- Never acquire another job's output claim while holding the Editor gate. Reservations survive until terminal cleanup/recovery.
- Use OS-backed exclusive locks, not check-then-create JSON files. Metadata contains jobId, PID, process start time, operation ID and session nonce.
- Heartbeats inform status only. Expired timeouts/heartbeats never authorize lock stealing.
- Queue Unity-dependent stages across jobs. Hold the project-wide Editor gate through Assets staging, refresh/import, compilation, build, registration, save, reload, verification and any cleanup.
- Editor renders/tests/readbacks also use the gate: rendering changes global shader state; reads during import can observe incomplete state.
- Establish one lock order: metadata transaction → release metadata → acquire Editor gate → short metadata transactions as needed. Never wait for Editor while holding metadata lock.
- Blender conversion runs in separate owned processes/sessions with distinct directories. Allocate ports atomically; verify bound PID + startup nonce + version/add-on handshake before any scene reset.
- Do not clear the user's shared Blender scene. A listener/port alone never proves ownership. Bound concurrent workers to configured resource capacity.
- Shared sources are read-only; initial implementation uses job-local conversion outputs, avoiding a shared mutable cache.

### 6. Registry safety, interruption and recovery

1. Before mutation, acquire Editor gate; verify project/Edit Mode, queued operation ownership, planned inputs and output baselines.
2. Reload shared registries from current saved state. Merge only owned keys; preserve entries added by jobs that completed while this job was converting.
3. Revalidate dependency fingerprints. Changed required template/schema/asset → INPUT_CHANGED and replan; unrelated registry entries do not invalidate the job.
4. Record journal: intended paths/keys, preimage hashes/GUIDs, intended operation, and progress after each save.
5. Build owned assets, update registrations, save, wait for imports/compilation, reload and verify. Publish final completion marker only after all required structural checks pass.
6. Release reservations/gate only when no queued or executing Editor mutation remains.

- Unity multi-asset saving is not atomic. Report partial application explicitly; do not promise transactional rollback.
- Caller timeout/disconnect → operation remains owned and status-queryable. Never issue a duplicate apply while its outcome is unresolved.
- Coordinator crash while Editor work continues → persistent unresolved-operation marker; restarted coordinator must reconcile Editor completion before admitting another writer.
- Use an Editor-side operation record/gate as well as OS locking; OS lock release after coordinator death alone is insufficient.
- Domain reload/Editor restart invalidates the live handshake; persistent journal requires reconciliation before queue execution resumes.
- Recovery requires original writer/process identity confirmed stopped and no active Editor operation. PID reuse is checked using process start time/session nonce.
- Prefer idempotent forward completion. Restore a preimage only if the current file still matches this job's recorded postimage; otherwise RECOVERY_CONFLICT.
- Never bulk Git reset/checkout, delete another job's outputs, or restore an entire stale shared registry.
- Mid-stage cancellation finishes/records a safe save boundary; ambiguous work stays needs-recovery. Terminating the waiting CLI does not cancel the Editor.
- Manual Editor actions, raw unity scripts and noncooperating agents cannot be made safe by advisory tooling alone. Require pipeline use during publication; detect known busy state/input drift and stop on conflicts. Document this limit clearly.

### 7. Cache, reports and token budget

- Stage fingerprint includes source bytes, normalized recipe, pipeline/converter versions, relevant Blender/add-on settings and upstream output hashes.
- Unity-stage fingerprint also includes template/dependency GUID+content fingerprints, project import settings and builder version.
- A cached stage is valid only when its complete manifest and output hashes verify. Write its completion marker atomically after all output writes close; partial results are never consumable. Report existence alone never means fresh.
- Changed source/recipe/tool/dependency invalidates downstream stages; missing or corrupt outputs invalidate their stage.
- Store full geometry arrays, logs and per-check details outside stdout. Default summary includes first actionable failures and links to full evidence.
- Record wall time, agent tool calls, generated script count and available token usage for matched baseline/new runs. Do not claim a percentage saving before measurement.
- Success target: ordinary supported new model requires a recipe, not new Python/C#; evidence and decisions remain auditable.

### 8. Migration stages and gates

- [x] Review full ALO guide, linked station guides, representative failure records and existing tools.
- [x] Define recipe/command/concurrency contracts and acceptance matrix; plan authored 2026-10-28.
- [ ] Phase 0 — capture current live asset/source baselines for representative imports; confirm source availability and current rules.
  - Gate: checked input inventory, existing GUIDs, registrations and gameplay snapshots; no historical record treated as current truth.
- [ ] Phase 1 — schema, CLI, immutable jobs, ownership reservations, OS/Editor gates, queue, journal and failure injection.
  - Gate: duplicate submission, lock race, timeout and recovery tests pass before any real asset publication.
- [ ] Phase 2 — extract source audit and static conversion; test against original binaries and known importer failures.
  - Gate: Rothana source/conversion checks; MC80 multi-slot/shader-collision fixture; explicit unsupported animation behavior.
- [ ] Phase 3 — shared Unity art, ship builder, derivatives and registration writer.
  - Gate: Rothana complete saved-asset parity; repeat application preserves GUIDs and produces no unintended serialized changes.
- [ ] Phase 4 — assembled-model support and Endurance recipe.
  - Gate: attachment transforms/material slots, component injection, mounts/hangars and full registration parity.
- [ ] Phase 5 — station adapter and Rebel levels 1–5 recipe.
  - Gate: distinct source levels, shared pivot, attached art, upgrade sequence and faction isolation.
- [ ] Phase 6 — simultaneous representative jobs, crash/recovery and stale-input acceptance.
  - Gate: matrix below passes with complete per-job evidence and no cross-job writes.
- [ ] Phase 7 — short agent README, guide routing, deprecation notes and measured workflow comparison.
  - Gate: agent completes a supported recipe-driven run without generating scripts or reading historical import logs.
- Keep legacy scripts until their migrated case passes; mark superseded entrypoints and route to the new tool. Remove only individually proven replacements.
- Do not mass-rebuild the fleet, rebalance units, or close unrelated import runtime-acceptance plans.

### 9. Three representative imports

Historical numbers below are regression seeds; Phase 0 verifies exact source variants and current saved assets before freezing expectations.

| Case | Required coverage | Acceptance |
|---|---|---|
| Rothana — static single-source ship | Verified Root restoration, hidden helpers, transport hierarchy, sparse HSV livery, full ship registration | Original 13 meshes / 42,813 triangles / 100 bones retained in source conversion; visible Hull_LOD_1 kept; geometry/UV/bones/materials pass; gameplay/preview/wreck/icons resolve to own assets |
| Endurance — assembled carrier | Hull + bridge + tower + 12 turret pieces; mixed material slots; template injection; multiple hangars | All 15 pieces and original parent transforms verified; no duplicated art; current 12-weapon/22-target baseline confirmed before migration; correct live/wreck paint, hangar semantics and own registrations |
| Rebel station — five levels | Five distinct ALOs, separate XML attachment art, common pivot, level bindings | Per-level attached artwork 5/8/11/14/18; unique models; shared anchors within 0.01 project units; transitions 1→5→2→3→4→5→1 preserve routing and update all dependent state |

- Rothana historical length 160 and dimensions 81.289 × 29.477 × 160 are comparison seeds, not permission to overwrite current tuning.
- Endurance historical source hashes 40 / texture copies 52; verify actual inventory, byte hashes and decoded pixel equality rather than accepting totals alone.
- Rebel has 20 unique attached ALO models; verify repeated instance transforms from original bone matrices, not FBX bone-axis rotation guesses.
- Preserve Rebel derived dome UV assets/repair metadata. Existing reported dome appearance remains unresolved; structural parity does not close that visual issue.
- Source unavailable → record exact missing path/hash and keep acceptance pending; do not silently substitute a model.

### 10. Fidelity and integration acceptance

- Compare original binary → Blender → FBX → Unity: source identities, hierarchy, transformed triangle corners, UVs, normals, per-slot triangles/shader/textures, attachment positions and visible bounds.
- Freeze units/tolerances before running: source-space geometry/bone tolerance 0.001; raw Unity geometry/bone 0.00002; UV max 0.025 source-texture pixels; nondegenerate normal-vector delta 0.001.
- These are proposed baseline ceilings, not measured results. Evaluate representability in Phase 0; any deviation needs a narrow documented exception before acceptance, never post-failure threshold inflation.
- Lossless PNG pixels must exactly match decoded DDS/TGA. All source hashes unchanged; material mappings/slot assignments exact.
- Helper topology/zero-area-face exceptions list exact affected source IDs. Preserve visible geometry even when names contain LOD.
- Reload saved prefabs/data: no missing scripts, broken mandatory references, accidental donor dependencies or invalid component injection.
- Verify target IDs, health entries, weapon arcs/muzzles, banking hierarchy, hangar capacity/reserve semantics and source anchor placement.
- Verify root scale/orientation, gameplay bounds and fitted collider/shield/ion/selection/navigation/launch geometry.
- Resolve all registrations by typed identity/GUID, including placement AssetReference in existing View group and all three icon consumers.
- Render geometry-only previews: transparent 512×512 icon; placement; live/wreck across all eight palettes; inspect silhouettes, crop and contrasting colors.
- Rerun apply twice: stable existing GUIDs, no duplicates, no unplanned path changes; no serialized diff on unchanged second run apart from evidence metadata.
- Import/serialization/compilation clean for changed scope. Runtime firing, hangar behavior, damage/abilities/fog/selection/death acceptance remains separately reported if not run.
- Before Unity tests follow current AGENTS.md scene inspection/preparation, asynchronous start and polling. Pipeline owns the Editor gate throughout; never open dialogs.

### 11. Concurrency acceptance matrix

| Scenario | Required result |
|---|---|
| Two models convert simultaneously | Separate Blender PID/nonce/port, requests, scenes, outputs and evidence |
| Two jobs apply different models | Unity stages queue; both registrations survive; no overlapping publication |
| Same model submitted twice | Same idempotency key returns existing operation; independent conflicting job fails before writes |
| Different model names, same output path or enum ID | Canonical ownership conflict rejected |
| Shared registry changed by earlier queued job | Reload/merge preserves both entries; no whole-file stale overwrite |
| Required template/source/recipe changes while queued | INPUT_CHANGED; no stale publication |
| Caller times out while Unity saves | Operation remains active/owned; second writer cannot enter |
| Coordinator dies before/during save | OS lock release cannot bypass unresolved Editor operation; recovery reconciles journal |
| Editor/domain reload or restart | Re-handshake, check persisted journal; interrupted job never falsely succeeds |
| Cancel during conversion/publication | Only owned Blender killed if safe; Editor completes safe checkpoint or stays unresolved |
| PID reused / stale heartbeat / orphan metadata | No timeout-only takeover; identity and quiescence verified |
| Partial failure after some registry writes | Recovery preserves unrelated/newer entries; partial state reported |
| Render/test submitted during import | Queued; no palette/state contamination or misleading readback |
| Missing cache output / changed bytes | Invalidated and rebuilt; existing report cannot bypass |
| Noncooperative external mutation detected | Stop with conflict; do not overwrite or claim concurrency guarantee |

- Stress test at least 3 simultaneous submissions, repeated 10 times with randomized stage delays; compare full output and registry ownership manifests.
- Inject crashes before publication, after asset save, after first registry save and before completion marker. Each must recover or report a precise conflict without false success.
- Verify locks/reservations released for completed/cancelled jobs and retained for unresolved mutations; queued jobs must not starve.

## Edge Cases

- Unsupported model capability → explicit failure with source evidence; add one tested shared capability only when justified.
- Source-mandated geometry can contain authored defects; retain source art and separately track approved derived repairs.
- Full-Armed hull + separate turrets can duplicate artwork; recipe/source audit must choose assembly intentionally.
- Existing unit enum additions and script compilation are shared mutations; distinct model output folders alone do not make imports independent.
- All artifacts may pass structural checks while runtime/visual review remains pending; report those gates independently.
- Plan completion requires implementation gates and evidence. Documentation authored today does not mark the tooling delivered.

## Files

- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]]
- [[AssetPipeline/Guides/SPACE_STATION_MODEL_IMPORT_GUIDE]]
- [[AssetPipeline/Guides/REPUBLIC_SPACE_STATION_MODEL_IMPORT_GUIDE]]
- [[AssetPipeline/Guides/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]]
- [[AssetPipeline/Imports/MC80 Independence Import]]
- [[TODOs/Tooling/Unified_ALO_Import_Pipeline - Research]]
- [[TODOs/TODOs]]

## TODO

- Execution authorized 2026-10-28; Phase 0–2 implementation is in progress. Keep all phase gates unchecked until their complete acceptance evidence passes.
- Confirm source availability, current schemas/paths and fixture baselines before writing recipes.
- Keep this plan active until all agreed tooling gates pass; do not close separate unit runtime-acceptance plans.
