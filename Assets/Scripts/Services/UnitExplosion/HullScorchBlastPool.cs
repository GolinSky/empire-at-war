using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// Prewarmed one-shot blasts on hull hits. Same pooling, anchor following and fog hiding as hardpoint
    /// explosions, with its own prefab and pool. Decides whether a blast plays: never for units hidden by fog, off
    /// camera or too small on screen, and never beyond <see cref="HullBlastLimits"/>; it never grows the pool.
    /// </summary>
    public sealed class HullScorchBlastPool : IHullScorchBlastPool, IInitializable, ITickable, ILateDisposable
    {
        // Unit intervals stretch once more than this share of MaxActive is in use; at full use they double.
        private const float LOAD_SHARE = 0.5f;

        private readonly HardPointExplosionPool _pool;
        private readonly HullBlastLimits _limits;
        private readonly UnityEngine.Camera _camera;
        private readonly float _prefabSize;
        private readonly Plane[] _planes = new Plane[6];

        private int _frame = -1;
        private int _startsThisFrame;
        private int _played;
        private int _skippedFog;
        private int _skippedOffscreen;
        private int _skippedSmall;
        private int _skippedCap;

        public HullScorchBlastPool(ExplosionVfx prefab, UnityEngine.Camera camera, HullBlastLimits limits)
        {
            _pool = new HardPointExplosionPool(prefab, camera, limits.MaxActive);
            _limits = limits;
            _camera = camera;
            _prefabSize = prefab.StartSize;
        }

        public float UnitInterval =>
            _limits.UnitInterval * Mathf.Max(1f, _pool.InUseCount / (_limits.MaxActive * LOAD_SHARE));

        public void Initialize() => _pool.Initialize();

        public void Tick() => _pool.Tick();

        public void LateDispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
                Debug.Log($"[VfxPools] HullBlasts played={_played} skippedFog={_skippedFog} " +
                    $"skippedOffscreen={_skippedOffscreen} skippedSmall={_skippedSmall} skippedCap={_skippedCap}");
#endif
            _pool.LateDispose();
        }

        public bool TryPlay(Transform anchor, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float scale)
        {
            if (fogVisibility != null && fogVisibility.IsHiddenByFog)
            {
                _skippedFog++;
                return false;
            }

            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                _startsThisFrame = 0;
                GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
            }

            if (_startsThisFrame >= _limits.MaxStartsPerFrame || _pool.IdleCount == 0 ||
                _pool.InUseCount >= _limits.MaxActive)
            {
                _skippedCap++;
                return false;
            }

            Vector3 position = anchor.position + worldOffset;
            float diameter = _prefabSize * scale;
            if (!IsInView(position, diameter * 0.5f))
            {
                _skippedOffscreen++;
                return false;
            }

            if (VfxScreenSize.Pixels(_camera, position, diameter) < _limits.MinScreenPixels)
            {
                _skippedSmall++;
                return false;
            }

            _startsThisFrame++;
            _played++;
            _pool.Play(anchor, fogVisibility, scale, anchor.InverseTransformVector(worldOffset), null);
            return true;
        }

        public void Clear() => _pool.Clear();

        private bool IsInView(Vector3 position, float radius)
        {
            for (int i = 0; i < _planes.Length; i++)
            {
                if (_planes[i].GetDistanceToPoint(position) < -radius) return false;
            }
            return true;
        }
    }
}
