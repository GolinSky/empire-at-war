# Squadron rebalance authoring

- Applied target sheet: `EmpireAtWarDocumentation/TODOs/Bugs/Carrier_and_Squadron_Rebalance.md`.
- `ApplyStats.py`: read-only serialized target inspection by default; requires PyYAML. `--apply` writes the fixed 2026-11-14 squadron/weapon targets, including baked counts. Follow disk writes with Unity refresh, explicit-path reserialization and SaveAssets.
- `ResizeSquadrons.cs`: one-time prefab authoring via `unity command run_script --file Tools/SquadronRebalance/ResizeSquadrons.cs --entry ResizeSquadrons.Apply --timeout_ms 60000 --json`. Reads target counts from data; clones complete craft, rewrites root bindings, saves prefabs and rebakes loadouts. Run only in an idle Editor after applying the matching data. Heroes and Delta-7 are excluded.
- `InspectAssets.cs`: read-only live asset report via `unity command run_script --file Tools/SquadronRebalance/InspectAssets.cs --entry InspectAssets.Inspect --json`. No gameplay execution or tests.
- `LiveInspection.json`: saved Editor asset inspection after the rework; includes all 21 squadron types, compiled profiles, counts, DPS and torpedo volleys. Not gameplay or test evidence.
- Changes to member counts require a fresh match; old saved member arrays fail the existing compatibility check. Host and client must use matching content.
