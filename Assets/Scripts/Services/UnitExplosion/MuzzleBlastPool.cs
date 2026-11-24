using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// Prewarmed one-shot blasts at firing muzzles. Same pooling, muzzle following and fog hiding as hardpoint
    /// explosions, with its own prefab and pool.
    /// </summary>
    public sealed class MuzzleBlastPool : IMuzzleBlastPool, IInitializable, ITickable, ILateDisposable
    {
        private readonly HardPointExplosionPool _pool;
        private readonly float _prefabSize;

        public MuzzleBlastPool(ExplosionVfx prefab, UnityEngine.Camera camera, int prewarmCount)
        {
            _pool = new HardPointExplosionPool(prefab, camera, prewarmCount);
            _prefabSize = prefab.StartSize;
        }

        public void Initialize() => _pool.Initialize();

        public void Tick() => _pool.Tick();

        public void LateDispose() => _pool.LateDispose();

        public void Play(Transform muzzle, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float diameter,
            Color color) =>
            _pool.Play(muzzle, fogVisibility, diameter / _prefabSize, muzzle.InverseTransformVector(worldOffset), color);

        public void Clear() => _pool.Clear();
    }
}
