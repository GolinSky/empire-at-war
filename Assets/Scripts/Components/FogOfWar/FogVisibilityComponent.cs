using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>
    /// Stops drawing an opponent unit while its position is covered by the player's fog of war.
    /// Uses <see cref="Renderer.forceRenderingOff"/> so it never fights code that toggles <see cref="Renderer.enabled"/>.
    /// The ion stun effect spawned at runtime is hidden with the unit. Bound only for opponent entities.
    /// Static buildings such as space stations stay drawn once discovered. Rechecked about once per second.
    /// </summary>
    public sealed class FogVisibilityComponent : MonoBehaviour, IInitializable, ISlowTickable, ILateDisposable,
        IFogVisibilityFacade, IRespawnable, IDespawnable
    {
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;
        private ISlowTickService _slowTick;
        private EmpireAtWar.Components.Combat.CombatModifiers _modifiers;

        [SerializeField] private Renderer[] renderers;
        [Tooltip("XZ footprint radius: the unit is revealed when vision reaches any part of it.")]
        [SerializeField, Min(0f)] private float revealRadius;
        [Tooltip("Once seen, stay drawn for the rest of the battle (static buildings).")]
        [SerializeField] private bool staysRevealedOnceSeen;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private IReadOnlyList<IIonStunViewSource> _ionStunSources;

        private bool _isHidden;
        private bool _isReleased;

        public bool IsHiddenByFog => _isHidden;

        [Inject]
        private void Construct(IVisionService visionService, ILocalPlayer localPlayer,
            IReadOnlyList<IIonStunViewSource> ionStunSources, EmpireAtWar.Components.Combat.CombatModifiers modifiers,
            ISlowTickService slowTick)
        {
            _slowTick = slowTick;
            _visionService = visionService;
            _localPlayer = localPlayer;
            _modifiers = modifiers;
            _ionStunSources = ionStunSources;
        }

        public void Initialize()
        {
            _renderers.AddRange(renderers);
            SubscribeSpawnedEffects();
            _isHidden = IsCoveredByFog();
            ApplyVisibility();
        }

        // Ion stun effects spawned in an earlier life stay tracked: they are reused, not spawned again.
        public void Respawn()
        {
            _isReleased = false;
            SubscribeSpawnedEffects();
            _isHidden = IsCoveredByFog();
            ApplyVisibility();
        }

        public void Despawn() => LateDispose();

        private void SubscribeSpawnedEffects()
        {
            foreach (IIonStunViewSource ionStunSource in _ionStunSources)
            {
                ionStunSource.IonStunViewSpawned += TrackIonStun;
            }

            _slowTick.Add(this);
        }

        public void LateDispose()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _slowTick.Remove(this);
            foreach (IIonStunViewSource ionStunSource in _ionStunSources)
            {
                ionStunSource.IonStunViewSpawned -= TrackIonStun;
            }
        }

        public void SlowTick()
        {
            if (_isReleased || staysRevealedOnceSeen && !_isHidden)
            {
                return;
            }

            bool isHidden = IsCoveredByFog();
            if (isHidden == _isHidden)
            {
                return;
            }

            _isHidden = isHidden;
            ApplyVisibility();
        }

        private bool IsCoveredByFog() =>
            _modifiers.IsCloaked || !_visionService.IsAreaVisible(_localPlayer.Id, transform.position, revealRadius);

        private void TrackIonStun(IIonStunRenderers ionStun)
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
