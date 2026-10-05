using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>
    /// Stops drawing an opponent unit while its position is covered by the player's fog of war.
    /// Uses <see cref="Renderer.forceRenderingOff"/> so it never fights code that toggles <see cref="Renderer.enabled"/>.
    /// Hardpoint explosion VFX and the ion stun effect spawned at runtime are hidden with the unit. Bound only for opponent entities.
    /// Static buildings such as space stations stay drawn once discovered.
    /// </summary>
    public sealed class FogVisibilityComponent : MonoBehaviour, IInitializable, ILateTickable, ILateDisposable
    {
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;

        [SerializeField] private Renderer[] renderers;
        [SerializeField] private HardPoint[] hardPoints;
        [Tooltip("XZ footprint radius: the unit is revealed when vision reaches any part of it.")]
        [SerializeField, Min(0f)] private float revealRadius;
        [Tooltip("Once seen, stay drawn for the rest of the battle (static buildings).")]
        [SerializeField] private bool staysRevealedOnceSeen;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private List<IIonStunViewSource> _ionStunSources;

        private bool _isHidden;
        private bool _isReleased;

        [Inject]
        private void Construct(IVisionService visionService, ILocalPlayer localPlayer,
            List<IIonStunViewSource> ionStunSources)
        {
            _visionService = visionService;
            _localPlayer = localPlayer;
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

            _isHidden = IsHiddenByFog();
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
            if (_isReleased || staysRevealedOnceSeen && !_isHidden)
            {
                return;
            }

            bool isHidden = IsHiddenByFog();
            if (isHidden == _isHidden)
            {
                return;
            }

            _isHidden = isHidden;
            ApplyVisibility();
        }

        private bool IsHiddenByFog() =>
            !_visionService.IsAreaVisible(_localPlayer.Id, transform.position, revealRadius);

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
