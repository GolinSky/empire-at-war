# Invincible

- Source: Republic at War Workshop `1129810972`; `Admiral_Trench` → `SeV_Invincible.ALO`. Source ALO/DDS/XML files are unchanged; full mod credits are retained in `Temp/InvincibleImport/Source/credits.md`. The mod also contains an unrelated `ReV_invincible.ALO` heavy cruiser.
- Separatist `ShipType.Invincible = 109`; hull `10000`, shields `6000`, `28` weapons and one shield-generator/engine/hangar system each. Existing Providence remains unchanged.
- Weapon profiles: `12` heavy dual turbolasers (`3`), `4` turbolasers (`4`), `8` medium dual turbolasers (`5`), `4` ion cannons (`11`). All `31` targets have unique IDs and explicit health/weapon/injection bindings; mounts use distinct authored sockets.
- Visible size `59.28171 × 109.37660 × 318.23566`; existing Providence length `114.23844`, ratio `39/14`. Bow `+Z`, up `+Y`, centered visual and unit-scale prefab roots. Bank `±10°`; hull Y `−54.68830..54.68830`; navigation radius `162`.
- Price `12000` and population `10` are twice Providence's values. Remaining movement, regeneration, range, hardpoint health, abilities, hangar reserves/capacity/timing, build `60 s`, tech `4`, limit `20` and non-hero roster flag are inherited.
- Selection/global-overlay anchor is `(0, -56.68830, 0)`, two units below the banked hull. The flat sprite stays centered on that anchor and is sized to the `324`-unit ring.
- Hull/wreck tint `(0.55, 0.58, 0.62)`, metallic `0.2`, smoothness `0.22`; source light tint `0.5`. Icon and live/wreck previews use these darker settings.
- Own visual/gameplay/data/shield/wreck/placement/icon/matchups and complete Separatist/Addressables/HUD/tooltip registrations. Source-alpha team mask covers `4.52621%` of the hull; normal green channel is flipped.

## Rebuild

1. `uv run --with pillow python Tools/Blender/Invincible/Prepare.py`
2. Run `Convert.py` in an isolated Blender `3.6.23` process with the installed ALAMO importer: `& "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" --background --factory-startup --python Tools/Blender/Invincible/Convert.py`.
3. `uv run python Tools/Blender/Invincible/VerifyGeometry.py`, then `uv run python Tools/Blender/Invincible/Stage.py`.
4. Run these entries sequentially with `unity command run_script --file Tools/Blender/Invincible/<Type>.cs --entry <Type>.Main --timeout_ms 60000 --json`: `BuildInvincibleArt`, `VerifyInvincibleUnityGeometry`, `BuildInvincibleShip`, `RenderInvincible`, `RegisterInvincible`, `VerifyInvincible`.
5. Check nested `data.result.success`; save updated reports from `Temp/InvincibleImport/` into `Evidence/` after a verified rebuild.

## Verification

- Seven transport meshes, `21421` triangles and `85` bones; visible hull/lights `9810/836` triangles. Original source index/parent transforms are checked independently, including repeated damage-bone names; identity Root restoration is binary-backed.
- Every FBX corner/UV checked: maximum displacement `0.000190735` source units, UV error `0`. All `64263` Unity corners checked: position error ≤`0.000008021` units; visible UV error `0`; bone error ≤`0.000005812` units, names/parents match.
- Upstream ALAMO importer welds shadow helpers; Unity remaps `83` unused Shadow UV corners by ≤`0.000994811`. Helpers and source weapon-flash planes remain in transport, absent from rendered game geometry. No source shader or ALA animation recreation is claimed.
- Providence has both full-arc turbolaser templates on the left. Invincible mirrors the right template's target position while preserving its profile/arc; `HardpointMapping.json` records the correction. All `28` weapons use authored sockets, `14` per side.
- Saved-reference, data/loadout, bounds, shield-plane, lifecycle, selection-anchor, material and doubled-economy verification passes. Eight live/wreck palettes rendered; blue/green appearance, hologram, icon and actual-size comparison inspected. Console has zero errors, MainMenuScene remains clean.
- No automated Unity tests, Play Mode combat or Addressables content build ran. Runtime combat/balance is unverified; a packed Player build needs its normal content rebuild.

## Files

- `Assets/Art/Models/SeparatistShips/Invincible/Invincible.fbx`
- `Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab`
- `Assets/Prefabs/Models/Ships/Separatist/InvincibleShipView.prefab`
- `Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset`
- Evidence: `Evidence/`; full corner data, packed blend, FBX and previews: `Temp/InvincibleImport/`.
