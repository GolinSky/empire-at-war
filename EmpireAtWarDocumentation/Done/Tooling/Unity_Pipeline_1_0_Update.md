---
category: Tooling
status: done
created: 2026-11-17
updated: 2026-11-20
completed: 2026-11-20
acceptance: pending-user
---
# Unity Pipeline 1.0 Update

## Goal
- Move `com.unity.pipeline` `0.6.0-exp.1` → `1.0.0` and `unity` CLI `1.0.0` → `1.0.1` without breaking agent tooling.

## Outcome (2026-11-20)
- CLI `1.0.1` (`unity --version`); package `1.0.0` in `Packages/manifest.json` + `packages-lock.json`; server reports `version 1.0.0`.
- Compile clean (`console get` → `compilationFailed=false`). All 17 project `[CliCommand]` tools listed in group `global` (note previously said 20; source has 17).
- Canonical guidance: `AGENTS.md` → `Unity CLI Argument Rules` → `Unity Pipeline 1.0 Commands`. Shared by Codex (direct), Claude (`CLAUDE.md`), Gemini/Antigravity (new `GEMINI.md`, `@AGENTS.md` import).
- MCP configs (`.mcp.json`, `.codex/config.toml`, `.gemini/`, `.antigravity/`, `.agents/`) all run `unity mcp --project-path .`; tool names follow the catalog automatically. No pinned names.
- Tests not run (local override).

## Old → New Commands (verified against live catalog)
| 0.6 | 1.0 |
|---|---|
| `list_open_scenes` | `scene list_open` |
| `run_tests` / `test_status` / `cancel_tests` / `list_tests` | `test run` / `test status` / `test cancel` / `test list` |
| `eval --code` | `script eval --code` |
| `eval_file --file` | `script eval --file` |
| `run_script --timeout_ms 120000` | `script run --timeout 120` (seconds) |
| `recompile_status` | `script recompile_status` |
| `get_console_logs` / `console` | `console get` (`--level log\|warn\|error`, `--tail`, `--since`) |
| `clear_console` | `console clear` |
| `editor_status` | `editor status` |
| project tools (`render_audit_capture`, …) | unchanged bare names; group `global`; MCP `mcp__unity__global_<name>` |

## Behavior Changes Verified
- Built-in params kebab-case (`--filter-type`, `--async-tests`, `--include-explicit`, `--dry-run`, `--output-path`); snake_case → exit `2` with "Did you mean".
- Project `[CliArg]` names keep declared snake_case (`timeout_seconds`, `threshold_ms`, …); no source change.
- `--json` prints raw result; no `success` / `data.result` wrapper. `script run` → `{"result": <entry return>, "diagnostics": []}`; `script eval --code 'return "pong";'` → `pong`, exit `0`.
- Throwing entry point → exit `6`. Bad param → exit `2`.
- `test run --timeout` enforced for synchronous runs only (`PipelineTestRunner.ExecuteSyncMode`); async runner ignores it. AGENTS.md wording kept.
- `Temp/pipeline_test_request.json` / `Temp/pipeline_test_status.json` names unchanged; status file written only on terminal states (`completed|error|cancelled`). `AloEditorOperationGate.cs` + `worker.py` gates still valid via the request file.
- `unity command --query <text>` is a catalog search; per-command params via `unity command <group> <name> --help`.

## Files Changed
- `Packages/manifest.json`, `Packages/packages-lock.json`.
- `AGENTS.md` (test safety, test selection, asset persistence, CLI rules, new 1.0 section); `GEMINI.md` (new).
- `Tools/AloImport/worker.py` (`script run`, `--timeout 120`); existing recursive `checked_result` + returncode check cover new shape.
- `Tools/Blender/SpaceStations/Update-StationArmament.ps1` (`script run`, `--timeout 120`, check `$result.result`).
- ~80 Tool READMEs/comments + vault `AssetPipeline/`, `Rules/BATTLE_PERFORMANCE_GUIDELINES`, active TODO notes: command/param renames; `nested result.success` → exit code.
- Not changed: `Stage.py`/`Package.py` `['success']` reads (they read local report files, not Pipeline output); `Evidence/*.json`, `Docs/Audits`, `Done/` plans (historical records).

## Verification
- Smoke: `editor status`, `scene list_open`, `script eval`, `script run` (probe + `Update-StationArmament.ps1 -Faction Empire -Inspect` end-to-end), `console get` — all exit `0`, no new console errors.
- `python -m py_compile Tools/AloImport/worker.py` OK.
- Not run: Unity tests (local override); ALO import worker end-to-end; Jenkins player build.

## Open Gates
- Jenkins: next player build of committed `main` succeeds (Pipeline DLLs excluded from non-development player since 0.7).

## Revert
- Reset manifest + lock to `0.6.0-exp.1`; `unity self-update --rollback`; revert the commit.

## Sources
- Registry `https://packages.unity.com/com.unity.pipeline`; changelog `docs.unity3d.com/Packages/com.unity.pipeline@1.0/changelog/CHANGELOG.html`; `unity self-update --changelog`; live `unity command pipeline list_commands --json` (2026-11-20).
