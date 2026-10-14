# Titan and Allegiance engine glow

- The ALO `MeshShield.fx` engine surfaces convert to disconnected bright rings, while their source alpha suppresses the intended exhaust glow in Unity.
- `FixEmpireEngineGlow.cs` disables only the imported glow renderers. It keeps the mechanical `Engine_Exhaust` meshes and adds fitted additive discs at the nozzle faces: Titan `6` white, Allegiance `3` blue and `4` amber.
- Run after either ship's art rebuild: `unity command run_script --file Tools/Blender/EmpireEngineGlow/FixEmpireEngineGlow.cs --entry FixEmpireEngineGlow.Main --json`.
- The script updates three existing materials, three mesh assets and the two visual prefabs. It can be run again without duplicating glow objects.
- Rear and angled Edit Mode preview renders were inspected on 2026-10-14. The original rings are hidden and all engine faces glow.
