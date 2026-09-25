# Ship weapon SFX

The eight WAVs in `Assets/Audio/SFX/Weapons` are original procedural sounds. Regenerate them with `python Tools/Audio/generate_weapon_sfx.py` (Python standard library only), then refresh/import in Unity. They are mono 44.1 kHz PCM, preloaded and decompressed on load; together they contain about 290 KiB of PCM data.

`AudioShipData.asset` maps all 14 `WeaponType` values to clips and volumes. The shared `WeaponAudio.prefab` has eight explicitly bound AudioSources routed through the existing Master mixer group. Sources use 2D playback because the presenter computes distance and stereo pan for the elevated RTS camera.

`WeaponHardPoint.EmitScheduledShot` publishes only after the projectile VFX is emitted. `WeaponComponent` forwards the event to `AudioShipComponent`, which routes it to the scene's `WeaponAudioPresenter`. The current `BoltShot` disables rate-based emission and calls `Emit(1)`; `ShotsPerSalvo` and `ShotInterval` therefore provide the firing cadence. No particle-count polling or audio per decorative particle is needed.

Rapid laser, point-defense, turbolaser, and ion salvos share a loop per ship and clip. Each actual emission extends its lifetime by the real-time shot interval plus an 80 ms tail. Playback lifetimes use unscaled time and freeze during pause, so accelerated game speeds do not truncate clips. Cancellation naturally expires the loop; disabling or releasing a ship stops its voices immediately. Heavy guns, torpedoes, missiles, and beams use discrete cues. Dropped requests are never queued for delayed playback.

The pure C# `WeaponAudioBudget` limits playback to eight voices, two per ship, three per shared clip, and three new starts per 60 ms. Simultaneous same-clip requests on a ship coalesce. The presenter caps the sum of source volumes at 1, reducing levels only when that ceiling is exceeded. Clip attack envelopes play immediately; only the end of a voice is faded. Pitch uses a separate random generator without affecting combat randomness, and active sources pause when game time is paused.

Tune `weaponMinDistance` (20), `weaponMaxDistance` (120), and `weaponZoomReferenceHeight` (180) on `AudioShipData.asset`. Distance is measured from the camera's center ray at the muzzle's height. Volume falls linearly to zero at the outer radius; zoom attenuation uses the square root of the reference-height ratio (0.6 at height 500). Individual weapon levels in `weaponSounds` account for the existing Master mixer's -10 dB attenuation.

The beam cue is 0.8 seconds, matching the current `BeamShot` growth and hold durations. If beam timing changes, regenerate that cue as well. Weapon audio does not change damage, projectile travel, or salvo scheduling.

## Ship abilities and engines

`Assets/Settings/Data/Models/Audio/ShipSfxData.asset` contains all seven ability profiles,
the engine loop, and acceleration cue. It is referenced by `AudioShipData.asset`.
Each profile has separate start, execution loop, end, and cooldown-restored clips,
with independent cue and execution levels. Proton Beam has a stronger execution
layer; other sustained effects sit below weapons and command dialogue.

The 30 WAVs in `Assets/Audio/SFX/Ships` are original synthesized sounds inspired by
the mechanical, military sci-fi language of RTS space battles. They do not sample
Empire at War. Regenerate them with `python Tools/Audio/generate_ship_sfx.py`, then
import the changed clips in Unity. The generator uses only the Python standard
library and preserves existing Unity metadata. Clips are mono 44.1 kHz PCM with
crossfaded loop boundaries and short envelopes on transients.

`ShipInstaller` creates the explicitly wired `ShipSfx.prefab` beneath each ship,
so all existing ship prefabs receive audio without individual prefab edits.
Its four AudioSources use the existing Master mixer: one cue source, one engine
source, and two execution sources matching the maximum concurrent ability slots
in the current ship data. If ships gain additional slots, add corresponding
sources to the prefab's `executionSources` array.

`ShipSfxPresenter` observes slot state changes: Active starts the activation cue
and execution loop; Recovering stops the loop and plays the end cue (including
cancellation); Recovering → Ready plays the restore cue once for player ships.
Normal cooldown ticks and initial Ready state produce no sound. Destruction and
view disable stop all voices and unsubscribe. Sources pause with game time.

`ShipEngineAudioModel` smooths measured sublight speed into engine volume/pitch
and detects acceleration, including an engine-power boost while moving. A 1.5 s
surge interval prevents rapid order changes from stacking acceleration sounds.
Hyperspace movement is excluded. This reactive engine replaces the old constant
ship ambience; alarm, hyperspace, dialogue and weapon audio keep their existing
channels.

Playback uses RTS camera-focus distance and stereo pan, with reduced gain when
zoomed out. Opponent ships hidden by fog are muted. Tune `minDistance`,
`maxDistance`, `zoomReferenceHeight`, `engineVolume`, and `accelerationVolume` on
`ShipSfxData.asset`. Ability cues are local to the ship; off-camera/out-of-range
cooldowns do not become global notifications.
