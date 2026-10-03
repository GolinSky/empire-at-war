using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>
    /// Stops drawing an opponent unit while its position is covered by the player's fog of war.
    /// Uses <see cref="Renderer.forceRenderingOff"/> so it never fights code that toggles <see cref="Renderer.enabled"/>.
    /// Hardpoint explosion VFX and the ion stun effect spawned at runtime are hidden with the unit. Bound only for opponent entities.
    /// </summary>
    public sealed class FogVisibilityComponent : MonoBehaviour, IInitializable, ILateTickable, ILateDisposable
    {
        private IFogOfWarSystem _fogOfWarSystem;

        [SerializeField] private Renderer[] renderers;
        [SerializeField] private HardPoint[] hardPoints;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private List<IIonStunViewSource> _ionStunSources;

        private bool _isHidden;
        private bool _isReleased;

        [Inject]
        private void Construct(IFogOfWarSystem fogOfWarSystem, List<IIonStunViewSource> ionStunSources)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _ionStunSources = ionStunSources;
        }

        public void Initialize()
        {
            _renderers.AddRange(renderers);
            foreach (HardPoint hardPoint in hardPoints)
            {
                hardPoint.ExplosionSpawned += TrackExplosion;
            }

            foreach (IIonStunViewSource ionStunSource in _ionStunSources)
            {
                ionStunSource.IonStunViewSpawned += TrackIonStun;
            }

            _isHidden = _fogOfWarSystem.IsHidden(transform.position);
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

            foreach (IIonStunViewSource ionStunSource in _ionStunSources)
            {
                ionStunSource.IonStunViewSpawned -= TrackIonStun;
            }
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

        private void TrackExplosion(ExplosionVfx explosion)
        {
            TrackRenderers(explosion.Renderers);
        }

        private void TrackIonStun(IonStunView ionStun)
        {
            TrackRenderers(ionStun.Renderers);
        }

        private void TrackRenderers(IEnumerable<Renderer> spawnedRenderers)
        {
            foreach (Renderer spawnedRenderer in spawnedRenderers)
            {
                spawnedRenderer.forceRenderingOff = _isHidden;
                _renderers.Add(spawnedRenderer);
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
