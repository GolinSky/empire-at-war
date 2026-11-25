using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Ship.EngineGlow
{
    /// <summary>
    /// Drives the ship's EngineGlow.shader renderers. A change writes one renderer user value per affected glow;
    /// the GPU fades from it every frame, so nothing runs on the CPU between changes.
    /// Each glow keeps its own value: glows of a destroyed engine hardpoint stay off while the rest follow the ship.
    /// These renderers have no other user value writer (UnitRenderStateComponent does not list them).
    /// </summary>
    public sealed class ShipEngineGlowComponent : MonoBehaviour, IShipEngineGlowComponent
    {
        private static readonly int IDLE_INTENSITY_ID = Shader.PropertyToID("_IdleIntensity");
        private static readonly int FADE_SECONDS_ID = Shader.PropertyToID("_FadeSeconds");

        [SerializeField] private EngineGlowRenderer[] glows;

        private uint[] _userValues;
        private bool[] _engineDestroyed;
        private EngineGlowState _state = EngineGlowState.Thrust;
        private float _idleIntensity;
        private float _fadeSeconds;

        // Entities are built inactive (BaseFactory), so ShipEngineGlowSync.Initialize runs before Awake would;
        // injection happens while the scope is built, ahead of every Initialize.
        [Inject]
        private void Construct()
        {
            _userValues = new uint[glows.Length];
            _engineDestroyed = new bool[glows.Length];
            if (glows.Length == 0) return;
            // Every glow of one ship shares these values (ShipEngineGlowPrefabTests).
            Material material = glows[0].Renderer.sharedMaterial;
            _idleIntensity = material.GetFloat(IDLE_INTENSITY_ID);
            _fadeSeconds = material.GetFloat(FADE_SECONDS_ID);
        }

        // The GPU Resident Drawer registers renderers on activation into reused instance slots, so a value
        // written while inactive can lose to the slot's previous unit (see UnitRenderStateComponent).
        private void OnEnable()
        {
            for (int i = 0; i < glows.Length; i++) glows[i].Renderer.SetShaderUserValue(_userValues[i]);
        }

        public void SetState(EngineGlowState state)
        {
            _state = state;
            // _Time.y in the shader is the time since level load.
            float now = Time.timeSinceLevelLoad;
            for (int i = 0; i < glows.Length; i++)
                if (!_engineDestroyed[i]) FadeTo(i, state, now);
        }

        public void SetEngineDestroyed(int engineHardPointIndex, bool destroyed)
        {
            float now = Time.timeSinceLevelLoad;
            for (int i = 0; i < glows.Length; i++)
            {
                if (glows[i].EngineHardPointIndex != engineHardPointIndex || _engineDestroyed[i] == destroyed) continue;
                _engineDestroyed[i] = destroyed;
                FadeTo(i, destroyed ? EngineGlowState.Off : _state, now);
            }
        }

        private void FadeTo(int index, EngineGlowState state, float now)
        {
            float level = EngineGlowUserValue.Evaluate(_userValues[index], now, _idleIntensity, _fadeSeconds);
            _userValues[index] = EngineGlowUserValue.Encode(level, state, now);
            glows[index].Renderer.SetShaderUserValue(_userValues[index]);
        }
    }
}
