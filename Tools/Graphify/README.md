# Automatic Graphify updates

This checkout uses Graphify 0.9.54 with automatic local code indexing. No Unity
commands, game-file writes, LLM calls, or API keys are involved.

## How it stays current

1. The per-user **empire-at-war Graphify** Windows task starts at sign-in. A
   one-minute trigger recovers an exited process; an already-running instance
   is left alone. It runs without elevation or a visible console.
2. `run.py watch` checks source contents every three seconds. After saves settle
   for one scan, it calls Graphify's existing update engine. Creation, edits,
   renames, deletion, checkout, pull/merge, and changes made while stopped all
   use the same reconciliation. Startup catches up immediately.
3. A content receipt avoids rebuilding unchanged code. Checks hash bytes, so
   restoring an old timestamp cannot hide edits. Graphify's stat-only hash
   shortcut is disabled in this process; its content-addressed AST cache stays
   enabled. At most two extraction workers run.
4. A Windows OS file lock serializes the watcher and all configured MCP clients.
   A second watcher exits. The OS releases locks if a process dies.
5. Each MCP graph access verifies the receipt against current source contents
   and the graph file. It updates on demand if the watcher has not caught up.
   Failed indexing returns a tool error, rather than results from the old graph.
   Edits during an update trigger another pass; persistent writes return an
   explicit retry error after three attempts. Graphify hot-reloads the new JSON.

This is automatic indexing, not instantaneous indexing: background updates start
after roughly 3–6 seconds of settled writes, plus build time. MCP queries wait
for freshness when needed. Existing MCP connections keep their old executable
until reconnected; their graph still receives the background updates.

## Scope

`watch.json` monitors `Assets/Scripts`, including uncommitted and untracked code.
Graphify's existing `.graphifyignore` still determines what enters the graph:
the generated input file and `Assets/Scripts/Editor` remain excluded. The broader
watch includes these paths so changing exclusions is detected. Source-control
ignore files and Graphify build settings are also monitored.

If expanding the graph to a new source tree, add that tree to `watch.json` as well
as permitting it in `.graphifyignore`. This flow guarantees freshness for the
declared code roots. It does not promise complete C# semantic understanding:
Graphify can report parser warnings, which are saved in `update.log`.

## Setup and status

Run once per clone/user (safe to repeat):

```powershell
& ./Tools/Graphify/Setup-Graphify.ps1
Get-ScheduledTask -TaskName 'empire-at-war Graphify'
$graphifyPython = (Get-Content graphify-out/.graphify_python -Raw).Trim()
& $graphifyPython Tools/Graphify/run.py status
```

`status` hashes the current files and reports `current: true` only when the
receipt matches. Runtime state is under ignored `graphify-out/service/`:

- `watcher.log`: starts, successful rebuilds, and failures (rotated).
- `update.log`: output from the latest background rebuild.
- `indexed.json`: last verified source hashes and graph stamp.
- `status.json`: last rebuild state; its PID belongs to that update, not
  necessarily the currently running watcher.

MCP-triggered update details go to the client's stderr. For a failed background
update, inspect both logs. The watcher retries automatically. To request a
diagnostic update through the same lock: `run.py update`. Do not run a separate
stock `graphify watch`, `graphify update`, or Graphify Git hook alongside this
flow: upstream 0.9.54's rebuild lock is a no-op on Windows.

To stop/remove only this service:

```powershell
Disable-ScheduledTask -TaskName 'empire-at-war Graphify'
Stop-ScheduledTask -TaskName 'empire-at-war Graphify'
Unregister-ScheduledTask -TaskName 'empire-at-war Graphify' -Confirm:$false
```

MCP query-time updating continues until its configuration is changed. Both
launchers require 0.9.54; review the adapter and repeat the integration checks
before upgrading because it uses Graphify's internal update and MCP cache APIs.

Run the isolated integration checks with `& $graphifyPython
Tools/Graphify/verify_flow.py`. They use a new temporary Git project outside Unity
and leave their path and `verification.json` in the output. They cover timestamp-
preserving edits, adds, renames, deletes, restart catch-up, duplicate watchers,
concurrent MCP queries, and refusing stale results after a rebuild failure.

## Documentation and reasons for the integration

- [Graphify recommended workflow](https://github.com/graphify-labs/graphify#recommended-workflow):
  commit/checkout hooks run asynchronously; pulls/merges and early queries can
  still need a manual update.
- [Graphify watch reference](https://github.com/graphify-labs/graphify/blob/v8/graphify/skills/agents/references/add-watch.md):
  automatic local code updates and debounce; docs/media need a separate semantic
  extraction workflow.
- Installed `graphify/watch.py`: the stock watcher has no initial reconciliation;
  `_rebuild_lock` falls back to no locking on Windows.
- Installed `graphify/cache.py`: the size/mtime shortcut can reuse an old hash
  after timestamp-preserving edits; reproduced by the integration check.
- Installed `graphify/serve.py`: graph contexts reload on JSON mtime/size changes.

The package files themselves are unchanged. No additional agent role, model,
subagent policy, or recurring Codex chat automation is installed.
