---
category: Features
status: in-progress
created: 2026-10-22
---
# Music Replacement Inventory

## Rules
- Verified 2026-10-22; all selected and excluded source paths exist.
- Paths below are relative to `F:/Temp_audio/Republic_at_War_1129810972/Audio`.
- Plan: [[Done/Features/Music_Replacement_Plan]].
- Battle destination strips `Space/Music/` and prefixes `Assets/Audio/Music/Battle/`.
- Menu destination strips either `Space/Music/` or `Galactic/Music/` and prefixes `Assets/Audio/Music/Menu/`.
- Counts: 22 battle + 31 menu = 53 MP3s. Expanded menu folders contain only MP3s.
- Execution must hash all sources and recheck this frozen list before importing.

## Files

### Battle — 22
- `Space/Music/Ahsoka/A_Floating.mp3`
- `Space/Music/Ahsoka/A_Loyalists_Edit.mp3`
- `Space/Music/Ahsoka/A_Loyalists.mp3`
- `Space/Music/Ahsoka/A_Not_the_Time_for_a_Lesson.mp3`
- `Space/Music/Ahsoka/A_Open_Fire.mp3`
- `Space/Music/Attack_of_the_Clones/AOTC_Bounty_Hunters_Pursuit_Edit.mp3`
- `Space/Music/Clone_Wars/CW_destroying_the_shield.mp3`
- `Space/Music/Other/space_ambient1.mp3`
- `Space/Music/Other/space_ambient2.mp3`
- `Space/Music/Other/space_battle1.mp3`
- `Space/Music/Other/space_battle2.mp3`
- `Space/Music/Other/space_battle3.mp3`
- `Space/Music/Other/space_battle4.mp3`
- `Space/Music/Other/space_battle5.mp3`
- `Space/Music/Other/space_battle6.mp3`
- `Space/Music/Revenge_of_the_Sith/ROTS_Grievous_and_the_droids_Edit.mp3`
- `Space/Music/Revenge_of_the_Sith/ROTS_Grievous_Speaks_to_Lord_Sidious_Edit.mp3`
- `Space/Music/The_Bad_Batch/TBB_Plan_99.mp3`
- `Space/Music/The_Bad_Batch/TBB_Walker_Chase.mp3`
- `Space/Music/The_Clone_Wars/TCW_Ahsoka_vs_Maul.mp3`
- `Space/Music/The_Clone_Wars/TCW_Battle_Of_Quell.mp3`
- `Space/Music/The_Clone_Wars/TCW_Y_Wing.mp3`

### Menu — 31
- `Space/Music/The_Bad_Batch/TBB_First_Time_in_Space.mp3`
- `Galactic/Music/Ahsoka/A_Senate_Hearing.mp3`
- `Galactic/Music/Ahsoka/A_The_Space_Whales.mp3`
- `Galactic/Music/Ahsoka/A_They_are_all_Connected.mp3`
- `Galactic/Music/Attack_of_the_Clones/AOTC_Across_The_Stars.mp3`
- `Galactic/Music/Clone_Wars/CW_the_jedi_council.mp3`
- `Galactic/Music/Obi_Wan_Kenobi/OW_Obi_Wan.mp3`
- `Galactic/Music/Other/gc_ambient1.mp3`
- `Galactic/Music/Other/gc_ambient2.mp3`
- `Galactic/Music/Revenge_of_the_Sith/ROTS_Padmes_Ruminations.mp3`
- `Galactic/Music/Revenge_of_the_Sith/ROTS_Palpatines_Teachings.mp3`
- `Galactic/Music/Revenge_of_the_Sith/ROTS_The_Birth_Of_The_Twins_And_Padme.mp3`
- `Galactic/Music/Revenge_of_the_Sith/ROTS_The_Immolation_Scene.mp3`
- `Galactic/Music/Revenge_of_the_Sith/ROTS_Yoda_And_The_Younglings.mp3`
- `Galactic/Music/Tales_of_the_Jedi/TOTJ_Fall_Of_Dooku.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Civil_War_About_to_Begin.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Debriefed.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Different_Perspectives.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Mayday.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Outpost.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Raxus_Under_The_Empire.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_So_Much_For_Peace.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Where_I_Fit.mp3`
- `Galactic/Music/The_Bad_Batch/TBB_Worth_More_Than_Jewels.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_Anakins_Theme.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_He_Is_the_Chosen_One.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_Jar_Jars_Introduction.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_Passage_Through_the_Planet_Core_Edit.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_The_Arrival_at_Tatooine.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_The_High_Council_Meeting.mp3`
- `Galactic/Music/The_Phantom_Menace/TPM_The_Trip_to_the_Naboo_Temple_and.mp3`

### Excluded
- `Galactic/Music/Other/Complete_Super_Ship.mp3`
- `Galactic/Music/Other/GC_Win.mp3`
- `Galactic/Music/Rebels/R_Its_Over_Now.mp3`
- `Galactic/Music/The_Old_Republic/**`
- All other Galactic music outside the menu allowlist.

### Legacy deletion manifest — 41
- All paths below are relative to `Assets/Audio/Music/Ambient/`; delete each asset with its metadata through Unity.
- `AOTC_Across_The_Stars.mp3`
- `AOTC_Ambush_On_Coruscant.mp3`
- `AOTC_Bounty_Hunters_Pursuit_Edit.mp3`
- `AOTC_Departing_Coruscant.mp3`
- `AOTC_Return_To_Tatooine.mp3`
- `AOTC_The_Tusken_Camp_The_Homestead.mp3`
- `CIS_Lose.mp3`
- `CIS_Win.mp3`
- `Complete_Ground_Defense.mp3`
- `Complete_Super_Ship.mp3`
- `CW_a_galaxy_divided.mp3`
- `CW_destroying_the_shield.mp3`
- `CW_dunes_of_tatooine.mp3`
- `CW_scaling_the_cliff.mp3`
- `CW_sneaking_under_the_shield.mp3`
- `CW_the_jedi_council.mp3`
- `gc_ambient1.mp3`
- `gc_ambient2.mp3`
- `opening.mp3`
- `RAW_Battle_Pending.mp3`
- `Republic_Tactical_Lose_Edit.mp3`
- `Republic_Tactical_Win_Edit.mp3`
- `ROTS_Edit.mp3`
- `ROTS_Grievous_and_the_droids_Edit.mp3`
- `ROTS_Grievous_Speaks_to_Lord_Sidious_Edit.mp3`
- `ROTS_Palpatines_Teachings.mp3`
- `space_ambient2.mp3`
- `space_battle1.mp3`
- `space_battle2.mp3`
- `space_battle3.mp3`
- `space_battle4.mp3`
- `space_battle5.mp3`
- `space_battle6.mp3`
- `space-ambient1.mp3`
- `Special_Epic_Chor.mp3`
- `Star_Wars_And_Revenge_Of_The_Sith.mp3`
- `TPM_Anakins_Theme.mp3`
- `TPM_Core_Edit.mp3`
- `TPM_He_Is_the_Chosen_One.mp3`
- `TPM_Passage_Through_the_Planet_C.mp3`
- `TPM_The_High_Council_Meeting.mp3`

## Implementation — Before Replacement
- `MusicAudioData.GetMusicList` returns menu list for MainMenu and faction dictionary entries otherwise.
- `MusicService` ignores Loading scene activation; picks random tracks and fades using unscaled time.
- `MusicService.Tick` calls PlayNext when AudioSource.isPlaying is false; background-loading startup requires verification before enabling asynchronous clip loading.
- MusicSource has no serialized starting clip, volume 0.5, looping off, priority 16 and Music mixer routing.
- Sample legacy importer `space_battle1.mp3.meta`: quality 1, loadType 0, no platform overrides, preload false, background loading false.
- Legacy reference scan: only `Assets/Settings/Data/Models/Audio/MusicAudioData.asset` matched old clip GUIDs across .asset/.prefab/.unity/.cs files.
- Engine ambience WAVs under `Music/Ship` are outside the deletion manifest; no use was inferred from their folder name.
- Source inventory and static reference checks completed; implementation, audio playback and builds not run.

## Implementation Evidence — 2026-10-22
- Frozen manifest: `Tools/Audio/MusicReplacementManifest.json`; 53 source → destination → role mappings with SHA-256/bytes, 41 legacy paths/GUIDs/hashes, configuration GUID and 4 retained Ship audio/metadata hashes.
- Rechecked source counts: 22 battle + 31 menu; all selected files present, no destination collisions; selected Space paths cover the entire Space music collection.
- Live legacy GUID scan across Assets (excluding audio binaries/metadata): only `MusicAudioData.asset`; no legacy string loads or direct Addressables entries found.
- `MainMenuScene` clean before execution. Windows standalone support installed; `StandaloneOSX` support absent. macOS build/runtime acceptance cannot run on this host.
- Unity configuration operation passed: 53 AudioImporters saved/reimported, 31 menu entries, 22 battle entries for each existing faction key, 41 explicit AssetDatabase deletions plus empty Ambient folder deletion. Configuration GUID preserved.
- Default + Standalone settings read back as Streaming/Vorbis/0.70/Preserve Sample Rate/preload off; background load on, mono/ambisonic off. Both Windows64 and macOS map to Standalone. macOS effective imported output remains unverified without its module.
- Windows active-target import artifacts: **145,963,764 bytes (139.20 MiB)** including serialized clip headers; streaming `.resource` payloads **143,888,192 bytes (137.22 MiB)**. Source MP3s remain **259,038,682 bytes** and match SHA-256. Sizes measured through `AssetDatabaseExperimental.LookupArtifact/GetArtifactPaths`; not a player-build size.
- First regression run: 7/8 passed. Legacy absence assertion initially used Unity's cached `GUIDToAssetPath`; corrected to `AssetPathToGUID(..., OnlyExistingAssets)` plus live serialized/metadata GUID scanning. Deleted files/metadata were already absent.
- Final `MusicReplacement` category: **8/8 EditMode tests passed**, 22.35 s; asset/source hashes, exact playlists through serialized data and runtime `GetMusicList`, retained Ship files, legacy absence, importer settings and pending-load/completion regressions.
- Windows build `build_9538bdf5bfc2`: Unity result **Succeeded**, **4,085,347,931 bytes (3.80 GiB)**, **833,515 ms**. Output: `Builds/MusicReplacementWindows/EmpireAtWar.exe`; full report captured locally in `Temp/MusicReplacement/windows-build-status.json`.
- Build reported 4 warnings + 1 unrelated shader error: `Assets/Art/Shaders/PlanetAutodeskInteractiveNoShadows.shadergraph(1755)`, MotionVectors/d3d11, invalid subscript `ObjectSpaceNormal`. Shader unchanged by this task; Windows build is not an error-free acceptance result.
- Player Pipeline bridge is disabled by existing configuration; no runtime instrumentation was enabled. No standalone player audition or battle-scene round trip was performed.
- No fresh successful Addressables layout was generated. Verified the actual shipped `model_assets_all_ff8f1ce3ac7fac22424373a21faf95a4.bundle` (**470,081,202 bytes**) with `AssetBundle.LoadFromFile` + `LoadAsset<MusicAudioData>`: exact 31-menu / 22-battle playlists for all 4 factions; all referenced clips have positive duration, Streaming load type and background loading. Model bundle includes other assets; its size is not soundtrack-only.
- Windows Editor Play Mode smoke passed: real menu startup/advancing samples; `Time.timeScale=0` + `AudioListener.pause=true` keeps music playing; Music mixer mute to -80 dB and restoration; forced track completion starts the next menu track; authored source volume returns to 0.5; exit clean.
- Observed menu playback `A_Senate_Hearing` → `ROTS_Padmes_Ruminations`; **1 playing source**, **2 scene music sources**. Existing idle `MusicSource(Clone)` is already serialized in `Assets/Scenes/MainMenuScene.unity`; preserved as unrelated scene content. Strict single-source acceptance remains open.
- One menu snapshot: `AudioUtil.GetFMODMemoryAllocated` **2,088,778 bytes**; `GetFMODCPUUsage` raw **0.674535632**. This is total FMOD state, not representative Audio Profiler memory/Streaming CPU acceptance.
- Restored only build-generated changes to the previously clean content-state binary and ProjectSettings; build-generated linker asset was removed by Unity. Addressables structure/configuration and scenes have no task diff; MainMenuScene remains clean.
