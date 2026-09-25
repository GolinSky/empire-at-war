# Shared Serena service

This checkout uses one Serena 1.7.0 HTTP backend at `http://127.0.0.1:9122/mcp`.
Codex and its applicable subagents connect to it without starting separate C#
language servers. The service belongs to the signed-in user and binds only to
loopback. It is dedicated to this checkout; never activate a different project
or worktree on this endpoint.

## Setup and lifecycle

From the repository root, run:

```powershell
./Tools/Serena/Setup-Serena.ps1
```

Setup uses `uv` and Python 3.13, installs the exact packages in
`requirements.lock` into `.serena/runtime`, applies the version-guarded C# patch,
installs `empire-at-war Serena.lnk` in the current user's Windows Startup
folder, and starts the service hidden. Package files are copied; the shared uv
cache is not patched. No administrator rights are required for Serena.

The sign-in shortcut runs `Start-Serena.ps1`. That launcher checks its recorded
process identity and uses a mutex to avoid duplicate starts. A foreign listener
on port 9122 is an error; the launcher does not terminate it. To start manually:

```powershell
./Tools/Serena/Start-Serena.ps1
```

To stop for maintenance:

```powershell
./Tools/Serena/Stop-Serena.ps1
```

Stop verifies the recorded executable and process creation time before stopping
only that service and its captured descendants. It does not stop other Serena,
Rider, Unity, or .NET processes. Stopping interrupts clients using this backend.
It starts again at the next Windows sign-in unless the shortcut is removed.
The launcher provides one start per sign-in; it is not a crash-restart watchdog.

Service state and current stdout/stderr are in `.serena/service/`. Detailed
Serena logs and its language-server cache remain in `.serena/home/`. These and
the dedicated runtime are ignored by Git. If the checkout moves, update its
Codex endpoint as needed and rerun setup to refresh the shortcut. Another
checkout needs its own service, port, and lifecycle configuration.

## C# adapter patch

`patch_csharp_adapter.py` applies a reviewed method replacement to the dedicated
environment only. It requires Serena 1.7.0 and the expected source block, fails
on drift, writes atomically, and recognizes an already applied patch.

The adapter opens root `empire-at-war.sln` when present, otherwise the first
root solution in deterministic name order. It sends only `solution/open`. If no
root solution exists, it sends `project/open` for sorted root C# project files.
It does not recursively discover projects inside Library, obj, or other nested
directories. Root Unity-generated solution and project files remain usable even
though Git ignores them. This patch is specific to this Unity project layout.

Roslyn can still restore projects inside the selected solution. The patch does
not claim to disable all restores. Sharing the backend prevents that initial
workspace load from being repeated for each connecting client.

Run the isolated regression tests without starting .NET:

```powershell
python -m unittest Tools.Serena.tests.test_csharp_adapter -v
```

For upgrades, stop the service, review the new adapter, update the patch and lock
file together, run the tests, then rerun setup. Do not simply loosen the version
guard or modify the uv cache.

## Codex configuration and migration

The repository-local `.codex/config.toml` connects to the shared HTTP service.
All Codex tasks in this checkout reuse this backend instead of launching a
Serena/C# stack per task. Project-specific agent roles remain disabled.
Do not restore stdio launch commands or activate another checkout on this service.

Already running Codex tasks can retain their old stdio configuration and
processes. Restart Codex after saving active work to load this configuration
consistently. Existing stdio process stacks are not terminated by setup.
The sign-in shortcut starts the shared service after Windows sign-in; the
launcher also starts it immediately during setup.

Before accepting an upgrade, connect two HTTP clients and verify one Roslyn
process, working C# symbols/references, no new restore batch on the second
connection, and continued service availability after one client disconnects.
