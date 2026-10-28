# Defense satellites

- Source: Workshop `1397421866`, `Turret_Space_Base` and six attachments for each of `QL`, `TL`, `Ion`, `Massdriver`, `Missile`.
- Blender 3.6.23/ALAMO; binary material slots and bone hierarchy restored. Source normals rounded to five decimals for bounded MCP requests.
- 31 FBX round trips preserve mesh/triangle counts, bone hierarchy, UVs and authored visibility. Maximum geometry error: `0.00000853` source units. See `ImportEvidence.json`.
- Unity FBX scale `0.02`, visual scale `5`; final satellite width `21.66–23.09` world units. Six weapons attach at source `turret_00..05`; firing positions use `MUZZLEA_00`.
- Gameplay: `1000` hull/shields, six targetable `300` HP mounts, no income. Existing weapon profiles determine damage/range. No Play Mode run.

## Rebuild

1. Run `Prepare.py` with Pillow available. Original ALO/DDS files stay unchanged.
2. Generate one request per model with `CreateConversionRequest.py <model>` and submit through Blender MCP. Save each `CONVERSION_JSON` result into the combined `Temp/DefenseSatellitesImport/ConversionReport.json` array.
3. Run `Stage.py`; use Unity `run_script` for `BuildArt.cs` (`BuildSatelliteArt.Main`), `BuildPlatforms.cs` (`BuildSatellitePlatforms.Main`), `Render.cs` (`RenderSatellites.Main`), `Register.cs` (`RegisterSatellites.Main`), then `IntegrateSites.cs` (`IntegrateSatelliteSites.Main`).
4. Builders save assets; they use existing Addressables groups. Registration adds no faction reinforcement entry: satellites are built on captured pads.
5. Run focused EditMode capture, satellite, AI, map, save and real entity composition checks. In-game visual/combat acceptance remains a user check.
