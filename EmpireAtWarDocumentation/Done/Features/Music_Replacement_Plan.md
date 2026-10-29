---
category: Features
status: done
created: 2026-10-22
completed: 2026-10-29
acceptance: pending-user
---
# Music Replacement Plan

> Closed 2026-10-29 by backlog triage: implementation complete; open user gates → [[TODOs/User_Acceptance_Queue]].

## Goal
- Replace all existing project soundtrack assets with the selected Republic at War collection: **22 battle + 31 menu MP3s**.
- Remove the legacy music collection completely from the current project; compress imported clips for Windows PC and macOS through Unity.
- Execution started 2026-10-22; desktop acceptance remains a separate verification gate.

## Rules
- Source root: `F:/Temp_audio/Republic_at_War_1129810972/Audio`. Preserve external originals.
- Battle: every MP3 recursively under `Space/Music`, except `The_Bad_Batch/TBB_First_Time_in_Space.mp3`.
- Menu: the 15 explicit files plus all 9 files in `Galactic/Music/The_Bad_Batch` and all 7 MP3s in `Galactic/Music/The_Phantom_Menace`.
- Import only the frozen allowlist in [[Done/Features/Music_Replacement_Plan - Research|Inventory and source evidence]]. Unlisted Galactic tracks are out of scope.
- Exclude `Galactic/Music/Other/Complete_Super_Ship.mp3`, `Galactic/Music/Other/GC_Win.mp3`, `Galactic/Music/Rebels/R_Its_Over_Now.mp3`, and the entire `Galactic/Music/The_Old_Republic` subtree.
- Menu exceptions never enter battle lists. No duplicate clip references within a playlist.
- Existing engine ambience WAVs under `Assets/Audio/Music/Ship` are SFX, not soundtrack replacement targets. Preserve SFX, voices, mixer controls and unrelated assets.

## Decision
- Destination: `Assets/Audio/Music/Battle/<source-series>/<filename>` and `Assets/Audio/Music/Menu/<source-series>/<filename>`; preserve original filenames.
- Keep `MusicAudioData.asset` and its GUID. Replace its lists; use the same 22 battle clips for every existing faction key. No faction-specific selection was requested.
- Replace the old menu list with all 31 selected menu tracks. Keep scene selection, random playback, current fades and music volume routing.
- Delete all 41 old MP3s and their metadata under `Assets/Audio/Music/Ambient`, including unused tracks and existing copies of newly selected recordings. Import selected sources at the new destinations; do not retain a legacy backup inside Assets.
- Why: one clear collection per playback context, complete removal of legacy assets, and unchanged runtime architecture.
- Avoid: a new playlist service, parallel menu ambience layer, external audio re-encoding, unrelated audio refactors, or rewriting Git history.

## Important Values
- Unity: `6000.4.7f1`.
- Sources: battle **72,212,008 bytes**; menu **186,826,674 bytes**; total **259,038,682 bytes (247.04 MiB)** before Unity import.
- Before replacement: **41 legacy MP3s**, **48,299,899 bytes**; 4 menu entries and faction keys `2,3,0,1` with `9,9,9,7` battle entries.
- After replacement: **31 menu entries**; faction keys remain `2,3,0,1`, each with the same **22 battle clips**.
- Compression target for every selected clip: **Vorbis**, quality **0.70**, **Streaming**, **Preserve Sample Rate**, **Force To Mono off**, **Ambisonic off**, **Preload Audio Data off**.
- Apply matching defaults and an explicit `BuildTargetGroup.Standalone` override; Windows and macOS use the desktop group. Verify accepted settings and effective imports for both targets.
- **Load In Background on**, with the playback loading-state check in step 3. Quality 0.70 is a proposed starting point, not an audited listening result.
- Compression controls Unity runtime/build data; source MP3 sizes in Assets remain unchanged. Record actual imported/build sizes; do not promise a fixed compression ratio.
- Basis: [Unity 6.4 Audio Clip import settings](https://docs.unity3d.com/6000.4/Documentation/Manual/class-AudioClip.html), [override API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AudioImporter.SetOverrideSampleSettings.html).

## Implementation
1. [x] **Freeze source and deletion manifests.**
   - Recheck all 53 allowlisted paths, counts, source hashes and destination collisions; record source → destination → role.
   - Snapshot all 41 old paths/GUIDs and scan their references, including Addressables entries, serialized assets and string loads.
   - Verify: no excluded or unlisted source; no missing input; preserve unrelated dirty work.
2. [x] **Import and configure the 53 clips.**
   - Create Battle/Menu folders per [[Architecture/PROJECT_ORGANIZATION]]; copy originals unchanged; refresh through official `unity` CLI.
   - Set AudioImporter settings through Unity APIs; check override result, reimport changed clips, read back effective desktop settings.
   - Verify: exactly 22 Battle + 31 Menu AudioClips, positive duration, expected channels, no decode/import errors.
3. [x] **Replace playlist references and validate streaming startup.**
   - Use SerializedObject to replace `menuMusicData` and each existing `battleMusicData` value; retain the configuration GUID and faction keys.
   - Check `MusicService.PlayNext/Tick` with background loading: a selected clip still loading must not trigger a new random selection each frame.
   - If reproduced, make a narrow loading-state change in MusicService; advance only after playback ends, and report failed loads explicitly. Add a focused regression test for pending-load versus finished playback if code changes.
   - Verify: every faction resolves the exact battle set; menu resolves the exact menu set; no old references or menu/battle overlap.
4. [x] **Remove the complete legacy collection.**
   - After rewiring, delete the 41 explicit legacy assets through AssetDatabase, including metadata; remove the empty Ambient folder through Unity.
   - Remove any obsolete direct Addressables entries through supported APIs only if found; preserve `Assets/AddressableAssetsData` structure.
   - Verify: old paths/GUID references absent across live Assets; no orphan metadata, missing references, duplicate old copies or imported exclusions.
5. [x] **Persist and run targeted validation.**
   - Save modified assets in the same Unity operation; refresh, selectively reserialize changed serialized assets, SaveAssets, and inspect Console.
   - Add/run focused playlist membership, exclusion, legacy absence and desktop importer assertions.
   - Before tests: `unity command list_open_scenes --json`; save dirty named scenes and recheck. Dirty untitled scene → `BLOCKED_DIRTY_UNTITLED_SCENE`. Run tests asynchronously and poll to completion.
   - Verify: saved asset data and settings survive re-read/reimport; relevant tests pass; no new import/serialization/compiler errors.
6. [ ] **Desktop acceptance and plan completion.**
   - Test menu → battle → menu, every faction, track completion, pause, volume/mute, startup, loading and exit; verify a single music source and correct playlists.
   - Audition quiet/loud passages and transitions against sources; tune Vorbis quality only where audible artifacts require it.
   - Build Windows and macOS with the necessary modules; verify soundtrack dependencies, effective import settings, build size and no legacy tracks. Run playback acceptance on both operating systems.
   - Record Audio Profiler memory/Streaming CPU and startup/transition behavior. Missing macOS runtime/build access stays an explicit pending gate, not a claimed pass.
   - Once all scope and verification pass, update this plan and backlog, move plan/research to Done/Features, and repair links.

## Files
- `Assets/Settings/Data/Models/Audio/MusicAudioData.asset` — existing playlist configuration; preserve identity.
- `Assets/Audio/Music/Ambient/` — complete legacy deletion scope.
- `Assets/Audio/Music/Battle/`, `Assets/Audio/Music/Menu/` — new soundtrack destinations.
- `Assets/Scripts/Services/Audio/MusicAudioData.cs`, `MusicData.cs` — existing data structure; no redesign planned.
- `Assets/Scripts/Services/Audio/MusicService.cs` — only conditional loading-state correction.
- `Assets/Prefabs/View/Audio/MusicSource.prefab` — existing source/mixer setup; retain.
- Related unfinished work: [[Done/Refactoring/Audio_System_Refactor_Plan]]; do not close that plan as part of asset replacement.

## Edge Cases
- Some new selections share filenames/audio with legacy assets → delete old assets, import the explicitly selected originals once.
- `space_ambient1.mp3` and `space_ambient2.mp3` stay in battle because the user selected the whole Space music folder.
- `A_Loyalists.mp3` and `A_Loyalists_Edit.mp3` are separately requested files; do not silently deduplicate versions.
- The explicit exclusion `Complete_Super_Ship.mp3` already has a legacy project copy → remove that copy too.
- Re-scan live references at execution time; the planning scan found legacy GUID references only in MusicAudioData.asset among .asset/.prefab/.unity/.cs files.
- No clean-scene, audio listening, runtime, test or build results were established during planning.

## TODO
- Steps 1–5 implemented and verified 2026-10-22; 8/8 MusicReplacement EditMode tests passed (22.35 s). No new audio import, serialization or compiler errors.
- Step 6 remains active: Windows build reports Succeeded (3.80 GiB) with an unrelated planet shader error; shipped music bundle membership/settings verified. Windows Editor menu startup, pause, mute/restore, track completion and exit smoke passed.
- Remaining: standalone Windows player audition; menu → battle → menu and every-faction scene acceptance; listening quality; representative Audio Profiler memory/Streaming CPU; strict single-source review (existing idle MainMenuScene clone preserved); macOS module/build/runtime unavailable.
- Evidence and measured import sizes: [[Done/Features/Music_Replacement_Plan - Research#Implementation Evidence — 2026-10-22]].
