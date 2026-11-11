# Unit dialogue

- Extracted source: `F:/Temp_audio/Empire_Rebellion_Audio/Audio`.
- Source catalog: `F:/Temp_audio/Empire_Rebellion_Audio/audio_catalog.csv`.
- Imported clips: `Assets/Audio/Voices/UnitDialogue/<Faction>/<VoiceSet>/`.
- Mapping asset: `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`.
- `UnitDialogueManifest.json` records exact source paths, SHA-256 hashes, subtitles and typed assignments.
- 690 WAVs / 20,854,398 source bytes; original filenames preserved.
- Import: mono, Vorbis quality 0.5, compressed in memory, preloaded, original sample rate.

## Routing

- `AudioDialogShipComponent` reads its injected `IUnitTypeFacade` once during construction.
- `ShipSfxData.GetVoiceSet(faction, unitType)` chooses a typed ship/squadron override, then the faction default.
- Generic Empire/Rebellion replies use `Unit_Select_*_Field`, `Unit_Move_*_Field` and `Unit_Attack_*_Field`.
- Specific ship sets: Victory variants, ISD I/II/III, Acclamator Assault, Tartan, Nebulon-B, Corellian Corvette, Mon Calamari/Home One/MC80 Independence, MC30c and Assault Frigate Mk II.
- Specific squadron sets: TIE Fighter, TIE Bomber, TIE Interceptor, X-Wing and Y-Wing.
- Unmatched units use their faction's generic officer; Republic/Separatist defaults remain unchanged.
- Executor uses the generic Empire officer: the extracted Executor set contains Vader speech and reprimands.
- Empire alarms share 11 combat lines from generic, ship, squadron, station and hero recordings.
- Rebellion alarms share eight combat lines from the generic officer, Assault Frigate, X-wing, Alliance Shuttle and Rebel space station.
- Each enemy-detection request can play one line through the shared voice channel and its two-second cooldown. The picker avoids replaying the last alarm when alternatives are available.
- Damage data uses short faction fighter damage reports; the damage property currently has no runtime consumer.
- Original XML events determine actions. `VSD` filenames belong to Star Destroyer events; `STD` filenames belong to Victory events.

## Verification

- 2026-11-01: all 682 imported clips referenced by the saved mapping asset; source hashes and importer settings verified.
- 15 ship overrides and 5 squadron overrides; every action reference resolves.
- Republic/Separatist voice arrays and all unrelated SFX values match the pre-change asset.
- Unity compilation completed without errors; Console reported zero errors after import/save.
- Tests not run under the local execution override. In-game listening acceptance remains pending.
