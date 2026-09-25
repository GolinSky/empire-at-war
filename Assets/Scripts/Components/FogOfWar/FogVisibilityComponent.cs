using System.Collections.Generic;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>
    /// Stops drawing an opponent unit while its position is covered by the player's fog of war.
    /// Uses <see cref="Renderer.forceRenderingOff"/> so it never fights code that toggles <see cref="Renderer.enabled"/>.
    /// Hardpoint explosion VFX spawned at runtime are hidden with the unit. Bound only for opponent entities.
    /// </summary>
    public sealed class FogVisibilityComponent : MonoBehaviour, IInitializable, ILateTickable, ILateDisposable
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private HardPoint[] hardPoints;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private FogOfWarSystem _fogOfWarSystem;
        private bool _isHidden;
        private bool _isReleased;

        [Inject]
        private void Construct(FogOfWarSystem fogOfWarSystem)
        {
            _fogOfWarSystem = fogOfWarSystem;
        }

        public void Initialize()
        {
            _renderers.AddRange(renderers);
            foreach (HardPoint hardPoint in hardPoints)
            {
                hardPoint.ExplosionSpawned += TrackExplosion;
            }

            _isHidden = _fogOfWarSystem.IsHidden(transform.position);
            ApplyVisibility();
        }

        public void LateTick()
        {
            if (_isReleased)
            {
                return;
            }

            bool isHidden = _fogOfWarSystem.IsHidden(transform.position);
            if (isHidden == _isHidden)
            {
                return;
            }

            _isHidden = isHidden;
            ApplyVisibility();
        }

        public void LateDispose()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            foreach (HardPoint hardPoint in hardPoints)
            {
                hardPoint.ExplosionSpawned -= TrackExplosion;
            }
        }

        private void TrackExplosion(ExplosionVfx explosion)
        {
            foreach (Renderer explosionRenderer in explosion.Renderers)
            {
                explosionRenderer.forceRenderingOff = _isHidden;
                _renderers.Add(explosionRenderer);
            }
        }

        private void ApplyVisibility()
        {
            foreach (Renderer entityRenderer in _renderers)
            {
                entityRenderer.forceRenderingOff = _isHidden;
            }
        }
    }
}
