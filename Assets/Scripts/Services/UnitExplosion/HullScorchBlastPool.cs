using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// Prewarmed one-shot blasts that cover new hull scorch marks. Same pooling, anchor following and fog hiding as
    /// hardpoint explosions, with its own prefab and pool.
    /// </summary>
    public sealed class HullScorchBlastPool : IHullScorchBlastPool, IInitializable, ITickable, ILateDisposable
    {
        private readonly HardPointExplosionPool _pool;

        public HullScorchBlastPool(ExplosionVfx prefab, UnityEngine.Camera camera, int prewarmCount)
        {
            _pool = new HardPointExplosionPool(prefab, camera, prewarmCount);
        }

        public void Initialize() => _pool.Initialize();

        public void Tick() => _pool.Tick();

        public void LateDispose() => _pool.LateDispose();

        public void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale) =>
            _pool.Play(anchor, fogVisibility, scale);

        public void Clear() => _pool.Clear();
    }
}
