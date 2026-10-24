# Desktop Asset Compression

Windows implementation evidence, 2026-10-11. The vault plan remains in progress.

- Unity `6000.4.7f1`, Windows x64, Direct3D 11, NVIDIA GeForce RTX 5070.
- Windows build report size: `2,762,002,718 → 2,082,952,003` bytes; saved `679,050,715` bytes (`24.6%`). Both builds succeeded with the same existing `PlanetAutodeskInteractiveNoShadows` MotionVectors shader error.
- Retained: 136 hull color/emission textures at Normal compression; pilot hull color, normal and team mask at Normal; one UI ship illustration at High Quality/BC7; one streamed Vorbis music track at quality 70%; one ADPCM weapon sound with Decompress On Load.
- Restored: 17 hull texture candidates that failed pixel screening, the world selection ring with no memory reduction, the precision-sensitive mesh, and the command voice at quality 100%.
- No resolution, sample rate, mono/stereo, mipmap, Read/Write, shader, material, source geometry, GUID, gameplay or global compression changes were retained.
- 162 importer paths read back, including rejected candidates. No new import errors during application. Two Windows players passed headless startup smoke checks; these do not test rendering, audio DSP or gameplay memory.
- Per-asset Editor texture memory arithmetic decreased by `996,358,349` bytes. This is not a matched gameplay memory capture.
- Eight team colors × two fixed camera views compared. Pixel screening uses linear GPU readback; batch acceptance requires RGB RMSE ≤ 2/255, maximum alpha error ≤ 8/255 and an Editor memory reduction. These conservative screens do not replace gameplay/Mac acceptance.

## Evidence

- `Evidence/Manifest.json`: old settings, tested candidates, restored exceptions, persisted readback and render comparisons.
- `Evidence/ReviewQueue.json`: untouched assets, consumer/precision/CPU-access review reasons, assets outside declared scene/Resources/Addressables roots.
- `Evidence/Summary.json`: build measurements, hardware/API and remaining acceptance.
- `output/DesktopAssetCompression/`: full original importer snapshots, build reports, comparison PNGs, startup logs and both player builds. Generated output is not included in the commit.
- Vault: `TODOs/Optimization/Desktop_Asset_Compression_Plan.md`.

## One-shot commands

Run through the official Unity CLI from the project root. Files stay outside `Assets` and compile into ephemeral assemblies.

```powershell
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionInventory.cs --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionRenderPilot.cs --args '["Before"]' --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionPilot.cs --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionBatchTextures.cs --args '[0,32]' --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionRenderPilot.cs --args '["After"]' --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionVerify.cs --json
```

- Inventory describes a specific baseline. Do not replay the old pilot/batches against already changed importers; the baseline comparison rejects that operation. Capture a fresh inventory before a new pass.
- Batch offsets used: `0, 32, 64, 96, 128`, each with count `32`. Eligibility is limited to uncompressed, non-readable model color/emission textures with verified Ship Lit/Ship Wreck consumers and no enabled desktop override.
- Restore an individual candidate by applying its `before` settings through the importer API, then `SaveAndReimport` and `AssetDatabase.SaveAssets`; read it back. Serialized importer JSON is an audit snapshot, not a reliable native-property rollback API.

## Pass 2 — 2026-10-24

- Fresh inventory, then `DesktopCompressionTexturePass2.cs` over every shipped, uncompressed, non-readable model texture with a material use (507): 462 retained (250 Normal color/mask, 50 High Quality/BC7, 162 normal maps at DXT5), 45 restored. Gates: color/mask RGB RMSE ≤ 2/255 and max alpha ≤ 8/255; normal maps decoded mean angle ≤ 1.5°, p99 ≤ 6°. Normal → High Quality → restore.
- `DesktopCompressionMeshPass2.cs` over 496 shipped models (MeshCollider sources and blend shapes excluded): 47 retained at Low, 449 restored. Gate: position ≤ 0.05 % of bounds diagonal, UV ≤ 1/4096, normal ≤ 2°. Low's max normal error is 2.5–2.8° on most hulls, so it fails the gate.
- Rerun at a 3° normal gate (user decision): 435 of the 449 restored models passed; 4 Rebel station source FBX (Levels 1, 3–5) put back to Off because `StationLevelViewTests` requires their baked `*Hull.asset` copies to equal the source exactly. 478 models now at Low; mesh payload 203 → 104 MB; bundles `1,025,898,126 → 989,838,944` bytes.
- `DesktopCompressionReconcile.cs` re-applied 38 importers whose `.meta` write hit a file lock; all 1,003 importers match the recorded outcome on disk.
- Addressables bundles `1,534,762,737 → 1,025,898,126` bytes; uncompressed payload 6,464 → 2,663 MB. Evidence: `Evidence/Pass2Summary.json`; per-asset results in `output/DesktopAssetCompression/Pass2/`.

```powershell
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionTexturePass2.cs --entry DesktopCompressionTexturePass2.Main --args '[0,40]' --timeout_ms 900000 --timeout 900 --json
unity command run_script --file Tools/DesktopAssetCompression/DesktopCompressionMeshPass2.cs --entry DesktopCompressionMeshPass2.Main --args '[0,50]' --timeout_ms 900000 --timeout 900 --json
```

## Remaining work

- Classify and pilot the remaining normal/mask/UI/world VFX/mesh/audio categories. Engine loops under `Music/Ship` are outside the current declared dependency set and remain unchanged; sharp UI remains unchanged.
- Audition retained music and weapon pilot, validate first playback, boundaries/crossfades and battle audio concurrency, and capture matched loading/memory/DSP CPU measurements before wider audio application.
- macOS build support is not installed. Resolve formats and validate on supported Mac hardware before declaring desktop acceptance complete.
- Fix the existing planet shader error in its own task.

API references: [texture format resolution](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/TextureImporter.GetAutomaticFormat.html), [effective audio sample settings](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AudioImporter.GetOverrideSampleSettings.html).
