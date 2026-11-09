# Unified ALO import tooling

Implementation and migration acceptance are in progress. Check `capabilities.json`
before submitting Unity stages. `apply` and `render` currently fail explicitly;
ship/station integration, registrations and derivatives have not passed their
migration gates. Keep the existing model scripts until those cases pass.

## Run

Install the versions in `requirements.txt` in the Python environment used for
every command. All commands emit one JSON envelope; detailed evidence stays in
the job directory. Exit 0 can mean queued: inspect `state`.

Create ignored `Tools/AloImport/local.json` with machine-specific configuration:

```json
{
  "sourceRoots": {"source-alias": "C:/absolute/source/directory"},
  "blender": "C:/absolute/path/to/blender.exe",
  "alamoAddon": "C:/absolute/path/to/io_alamo_tools",
  "blenderWorkers": 2
}
```

Use a complete reviewed recipe; the synthetic test fixture is not a model recipe.

```powershell
python Tools/AloImport/alo_import.py doctor --config Tools/AloImport/local.json --json
python Tools/AloImport/alo_import.py inspect --recipe Tools/AloImport/Recipes/<Model>.json --config Tools/AloImport/local.json --json
python Tools/AloImport/alo_import.py plan --job <job-uuid> --json
python Tools/AloImport/alo_import.py convert --job <job-uuid> --json
python Tools/AloImport/alo_import.py status --job <job-uuid> --json
python Tools/AloImport/alo_import.py verify --job <job-uuid> --json
```

- `--wait-seconds N` waits for the worker; default 0 returns immediately.
- Recipe or tool changes require a new immutable job. No edits to `Input.json`.
- `resume` reconciles ownership and can requeue interrupted conversion. It never
  blindly repeats a Unity mutation.
- `cancel` cancels queued stages and requests a safe boundary for active work.
- `recover` checks original writer identity and terminal Editor evidence. Missing
  or conflicting completion records require explicit Editor reconciliation.
- Logs and per-stage results: `Tools/AloImport/.state/Jobs/<UUID>/`.

## Ownership and recovery

- Sources are read-only. Python conversion writes only owned job staging.
- Blender runs in an owned factory-startup background process; no shared Blender
  scene or MCP listener is reused. The checkout-wide worker pool bounds capacity.
- Output paths, `.meta` paths and unit names/values are reserved under an OS lock.
  Physical paths include junction/symlink resolution and Windows case handling.
- Unity stages share an OS Editor gate and a persistent Editor-side operation
  gate. Metadata locks are released before waiting for Editor access.
- Caller timeout or death does not establish Editor quiescence. The unresolved
  marker blocks another writer until matching completion evidence is reconciled.
- PID plus process creation time identifies a live owner. Heartbeat age never
  authorizes takeover. Recovery never restores a whole stale registry or uses Git
  reset/checkout.
- Durable state lives outside Unity's `Temp` so Editor restart/cleanup cannot erase
  ownership, preimages or completion records. `layout.json` is shared by both runtimes.
- Coordination covers cooperating local tools on one physical checkout. Manual
  Editor edits and other writers can still conflict; shared-checkout operation
  across multiple machines is unsupported.

## Verification

```powershell
python -m unittest discover -s Tools/AloImport/tests -v
```

Test execution and test preparation are disabled for agents by `AGENTS.md` unless
the user explicitly requests tests. Use source inspection and non-test compilation.
The command above is only for an explicitly authorized test run.

Python tests use temporary checkouts, original-format binary fixtures and child
processes. They include conflicting reservations, three concurrent submissions
repeated ten times, dead writers, nested Unity failure envelopes and stale inputs.
They do not prove real multi-unit Unity registration or recovery during saves.

`AloEditorOperationTests` has category `Core`. Follow repository scene preparation
and asynchronous test rules before running it. Saved-file/dependency checks,
source fidelity, full integration, visual review and runtime acceptance are
separate results; no report implies an unrun gate passed.

Baseline capture is read-only: `python Tools/AloImport/capture_baseline.py`.
`Evidence/Baseline.json` records current original-file hashes and saved asset
GUID/hash inventory, including its limits. Detailed job geometry and logs remain
under `Temp`; retain only compact intentional acceptance evidence here.
