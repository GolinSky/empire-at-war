---
category: Tooling
status: todo
created: 2026-11-17
---
# Unity Pipeline 1.0 Update

## Goal
- Move `com.unity.pipeline` `0.6.0-exp.1` → `1.0.0` (stable, 2026-11-11) without breaking agent tooling.

## Current State (2026-11-17)
- Package: `0.6.0-exp.1` in `Packages/manifest.json`; skipped 0.7, 0.8, 0.9.x, 0.10, 1.0.0-pre.1.
- CLI `unity` = `1.0.0` already → CLI/package already mismatched; update lowers that risk.
- Editor `6000.4.7f1`; 1.0.0 requires `6000.0`+. Dependencies identical to 0.6.
- Registry `latest` = `1.0.0`.

## Verdict
- Safe to update **with a doc/tooling sweep**. Project C# compiles unchanged; breakage is in command names used by docs and scripts.

## Breaking Changes That Hit Us
- 0.9: built-in commands grouped (`unity command <group> <name>`); 1.0: old ungrouped names removed.
  - `run_script` → `script run`; `eval` → `script eval`; `run_tests` → `test run`; `test_status` → `test status`; `list_open_scenes` → `scene …` (check `unity command --query`).
  - `eval_file`, `capture screenshot`, `get_console_logs` removed → `script run`, `capture game_view`, `console`.
- 1.0: `script eval` returns the raw value; failure = exit code `6`, no `success` field.
- 0.10: unknown params refused, not ignored; `test run` defaults to editor mode; `test status` uses camelCase (`summary.total`, `results[].fullName`).
- 0.10: server settings move `ProjectSettings/` → `UserSettings/` (auto).
- 0.7: Pipeline runtime + Roslyn DLLs dropped from non-development players (good; check Jenkins release build).

## Not Affected
- `Assets/Scripts/Editor/RenderAudit*.cs`: `[CliCommand]`/`[CliArg]` still in `Unity.Pipeline.Commands`, attribute asmdef `Unity.Pipeline.Attributes` is `autoReferenced` → Assembly-CSharp-Editor compiles. Undotted names → `global` group, still callable as `unity command render_audit_capture`.
- Project has no asmdef referencing Pipeline assemblies.

## Callers To Update
- `AGENTS.md`: `list_open_scenes`, `run_tests --filter_type …`, `test_status`, `eval --code`.
- `Tools/AloImport/worker.py:54,86` (`run_script`) — programmatic, will fail hard.
- `Tools/BattleAudit/README.md`, `battle_audit.py` (`eval_file`, `get_console_logs`).
- ~40 `Tools/Blender/*/README.md`, `Tools/BattleSimulator/README.md` (`run_script`, `run_tests`).
- Scripts that read `data.result.success` from `eval` output.

## Steps
1. [ ] Commit or stash current dirty tree first; update on a clean `main` commit for easy revert.
2. [ ] `unity command package add` / edit manifest to `com.unity.pipeline: 1.0.0`; let Unity resolve; confirm `packages-lock.json`.
3. [ ] Check console: zero compile errors; RenderAudit commands listed (`unity command --query render_audit_capture --json`).
4. [ ] Map each old name to its 1.0 name with `unity command --query <name> --json`; record table here.
5. [ ] Update `AGENTS.md` test-safety/selection commands, `worker.py`, BattleAudit, then sweep READMEs (`grep -rn "unity command [a-z]*_"`).
6. [ ] Smoke: `editor status`, `scene list` equivalent, `script eval --code 'return "pong";'`, one `script run` of an existing Tools script. No tests (local override).
7. [ ] Jenkins: confirm next player build of committed `main` succeeds.
8. [ ] Revert path: reset manifest + lock to `0.6.0-exp.1`.

## TODO
- Confirm exact 1.0 name for `list_open_scenes`.
