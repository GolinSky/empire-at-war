---
category: Tooling
status: todo
created: 2026-11-17
updated: 2026-11-19
---
# Unity Pipeline 1.0 Update

## Goal
- Move `com.unity.pipeline` `0.6.0-exp.1` → `1.0.0` (stable, 2026-11-11) without breaking agent tooling.
- Move `unity` CLI `1.0.0` → `1.0.1` in the same pass.

## Current State (verified 2026-11-19)
- Package: `0.6.0-exp.1` in `Packages/manifest.json` + `packages-lock.json`; skipped 0.7, 0.8, 0.9.x, 0.10, 1.0.0-pre.1.
- Registry `latest` = `1.0.0`. No `1.0.1` / `1.1` package exists; changelog top entry is 1.0.0.
- New since 2026-11-17: CLI `1.0.1` available (`unity self-update --check` → `1.0.0 → 1.0.1`). Installed CLI = `1.0.0`.
- Editor `6000.4.7f1`; 1.0.0 requires `6000.0`+. Dependencies identical to 0.6.
- Live command catalog is still 0.6: flat names (`run_script`, `eval`, `run_tests`), snake_case params (`--filter_type`, `--timeout_ms`, `--dry_run`).
- No `ProjectSettings/*Pipeline*` server settings file, no `ENABLE_RUNTIME_PIPELINE` define, no `EditorPipelineManager.asset`.

## Verdict
- Safe to update **with a doc/tooling sweep**. Project C# should compile unchanged.
- Sweep is larger than first recorded: parameter names change too, not only command names.

## Breaking Changes That Hit Us
- 0.9: built-in commands grouped (`unity command <group> <name>`); 1.0: old ungrouped names removed, no redirect.
  - Confirmed in changelog: `script eval`, `script run`, `test run`, `test status`, `test cancel`, `console`, `capture game_view`, `capture scene_view`, `editor status`, `pipeline list_commands`.
  - Removed: `script eval_file` (1.0, no replacement named), `capture screenshot` (1.0), `get_console_logs` → `console` (0.7).
  - Not in changelog, confirm after update: `list_open_scenes`, `list_tests`, `recompile_status`, `clear_console`.
- 0.10: **parameters are kebab-case**; old spellings refused, unknown params refused (not ignored).
  - `--filter_type` → `--filter-type`; `--async_tests` → `--async-tests`; `--include_explicit` → `--include-explicit`; `--timeout_ms`, `--dry_run`, `--save_path` likewise. Confirm each with `--query`.
  - Files a command writes → `output-path`; relative capture paths resolve from project root, not `Assets/`.
  - Reserved, cannot be a command parameter: `format`, `json`, `fields`, `verbose`, `omit-nulls`. Project `[CliArg]` names use none of them.
- 1.0: `script eval` returns the raw value; failure = exit code `6`, no `success` field.
- 1.0: built-ins report failure through exit code; batch timeout = `14`; cancelled `test status` = `130`.
- 1.0: `test run` defaults to editor mode; mode `all` now allowed with `async-tests`; replies camelCase (`summary.total`, `results[].fullName`); non-zero exit on bad `filter_type`, no match, or any failure.
- 0.9: timeouts per command via `--timeout` → AGENTS.md line "async runner does not enforce `--timeout`" needs re-check.
- 0.8: servers bind and answer only `127.0.0.1`, not `localhost`.
- 0.10: server settings `ProjectSettings/` → `UserSettings/` (auto; nothing to migrate here).
- 0.7: Pipeline runtime + Roslyn DLLs dropped from non-development players (good; check Jenkins release build).

## CLI 1.0.1
- Piped output (`--format tsv` default) gains a header row for several commands → scripts must pass `--json` or `--format human`. Our programmatic callers already pass `--json`.
- `unity mcp --tools` / `--exclude-tools` accept a whole command group.
- No change to `unity command` argument syntax.
- Apply: `unity self-update` (needs user approval); rollback `unity self-update --rollback`.

## Project Commands
- 20 `[CliCommand(name, description)]` commands in 7 files: `RenderAudit*.cs` (5 files), `BattleAttributionRun.cs`, `BattleMemoryCapture.cs`, `BattleProfilerFlightRecorder.cs`.
- Attributes stay in `Unity.Pipeline.Commands`; asmdef `Unity.Pipeline.Attributes` is auto-referenced; project has no asmdef referencing Pipeline → compiles.
- Undotted names → `global` group, callable by bare name (0.9; form un-deprecated in 0.9.1).
- Unverified: custom `[CliArg]` snake_case names (`timeout_seconds`, `threshold_ms`, `max_saves`, `gc_stacks`, `live_export`, `frame_debugger_frames`, `duration_seconds`, `max_events`) — may need kebab-case on the command line or in source.

## Callers To Update
- `AGENTS.md:243-261,287,291,302,322`: `list_open_scenes`, `run_tests`, `test_status`, `cancel_tests`, `eval --code`, `--filter_type`, `--include_explicit`, `--async_tests`, "`all` mode unsupported".
- Programmatic, fail hard:
  - `Tools/AloImport/worker.py:54,86` (`run_script`); `:21` checks nested `success`.
  - `Tools/Blender/SpaceStations/Update-StationArmament.ps1:12,16` (`run_script`, `--timeout_ms`, `data.result.success`).
  - `Tools/Blender/{AotrEmpressStation/Package.py, Bulwark, Invincible, Peleset, Recalcitrant, Wavecrest}/Stage.py` read `result.success`.
- `Temp/pipeline_test_status.json` read by `worker.py:172` and `Assets/Scripts/Editor/AloImport/AloEditorOperationGate.cs:32` — internal file; confirm name and `status == "running"` still hold.
- Docs: `run_script` in 79 files (64 under `Tools/Blender`); `test_status` 14; `list_open_scenes` 12; `eval --code` 12; `run_tests` 9; `eval_file` 6; `get_console_logs` 5 (`Tools/BattleAudit`).
- Vault guides: `AssetPipeline/` (3 notes with `run_script` / `result.success`).
- MCP: `unity mcp` tool names follow the catalog (`mcp__unity__run_tests` → grouped name). No allowlist in `.claude`, `.mcp.json`, `.codex/config.toml` pins them.

## Steps
1. [ ] Commit or stash current dirty tree first; update on a clean `main` commit for easy revert.
2. [ ] `unity self-update` → `1.0.1`; confirm `unity --version`.
3. [ ] Set `com.unity.pipeline: 1.0.0` in manifest; let Unity resolve; confirm `packages-lock.json`.
4. [ ] Check console: zero compile errors; all 20 project commands listed (`unity command --query render_audit_capture --json`, `battle_hitch_profiler_arm`).
5. [ ] Dump new catalog (`unity command pipeline list_commands --json`); record old → new command and parameter table here. Settle the unverified names above.
6. [ ] Check project `[CliArg]` spelling with one call: `unity command render_audit_frame_debugger --timeout_seconds 15` vs `--timeout-seconds 15`. Rename in source only if required.
7. [ ] Update `AGENTS.md`, then `worker.py`, `Update-StationArmament.ps1`, the 6 `Stage.py`/`Package.py` success checks, BattleAudit.
8. [ ] Sweep READMEs and vault guides: `grep -rnE "unity command [a-z]+_|--[a-z]+_[a-z]+"` over `Tools`, `AGENTS.md`, vault; exclude `node_modules`.
9. [ ] Smoke: `editor status`, open-scenes listing, `script eval --code 'return "pong";'` (expect raw `pong`, exit `0`), one `script run` of an existing Tools script, `console`. No tests (local override).
10. [ ] Jenkins: confirm next player build of committed `main` succeeds.
11. [ ] Revert path: reset manifest + lock to `0.6.0-exp.1`; `unity self-update --rollback`.

## TODO
- Exact 1.0 names for `list_open_scenes`, `list_tests`, `recompile_status`, `clear_console`.
- Replacement for `eval_file` in `Tools/BattleAudit` (likely `script run`, not verified).
- Whether `--timeout` is now enforced for async `test run`; update AGENTS.md wording.

## Sources
- Registry `https://packages.unity.com/com.unity.pipeline` (2026-11-19).
- Changelog `docs.unity3d.com/Packages/com.unity.pipeline@1.0/changelog/CHANGELOG.html` (0.7 – 1.0.0 read in full).
- `unity self-update --changelog` (CLI 1.0.1).
