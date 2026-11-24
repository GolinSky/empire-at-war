using UnityEngine;

namespace EmpireAtWar.Components.Ship.EngineGlow
{
    /// <summary>
    /// Drives the ship's EngineGlow.shader renderers. A state change writes one renderer user value per glow;
    /// the GPU fades from it every frame, so nothing runs on the CPU between changes.
    /// These renderers have no other user value writer (UnitRenderStateComponent does not list them).
    /// </summary>
    public sealed class ShipEngineGlowComponent : MonoBehaviour, IShipEngineGlowComponent
    {
        private static readonly int IDLE_INTENSITY_ID = Shader.PropertyToID("_IdleIntensity");
        private static readonly int FADE_SECONDS_ID = Shader.PropertyToID("_FadeSeconds");

        [SerializeField] private MeshRenderer[] glowRenderers;

        private uint _userValue = EngineGlowUserValue.SETTLED_THRUST;
        private float _idleIntensity;
        private float _fadeSeconds;

        private void Awake()
        {
            if (glowRenderers.Length == 0) return;
            // Every glow of one ship shares these values (ShipEngineGlowPrefabTests).
            Material material = glowRenderers[0].sharedMaterial;
            _idleIntensity = material.GetFloat(IDLE_INTENSITY_ID);
            _fadeSeconds = material.GetFloat(FADE_SECONDS_ID);
        }

        // The GPU Resident Drawer registers renderers on activation into reused instance slots, so a value
        // written while inactive can lose to the slot's previous unit (see UnitRenderStateComponent).
        private void OnEnable() => WriteUserValue();

        public void SetState(EngineGlowState state)
        {
            if (glowRenderers.Length == 0) return;
            // _Time.y in the shader is the time since level load.
            float now = Time.timeSinceLevelLoad;
            float level = EngineGlowUserValue.Evaluate(_userValue, now, _idleIntensity, _fadeSeconds);
            _userValue = EngineGlowUserValue.Encode(level, state, now);
            WriteUserValue();
        }

        private void WriteUserValue()
        {
            foreach (MeshRenderer glowRenderer in glowRenderers) glowRenderer.SetShaderUserValue(_userValue);
        }
    }
}
