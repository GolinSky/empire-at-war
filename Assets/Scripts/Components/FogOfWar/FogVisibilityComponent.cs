using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Components.Combat;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>
    /// Draws an opponent ship while its hull overlaps vision or a firing reveal is active.
    /// Uses <see cref="Renderer.forceRenderingOff"/> so it never fights code that toggles <see cref="Renderer.enabled"/>.
    /// The ion stun effect spawned at runtime is hidden with the unit. Bound only for opponent entities.
    /// Static buildings such as space stations stay drawn once discovered. Ships recheck fog at 10 Hz; others at 1 Hz.
    /// Cloak and firing-reveal changes are applied immediately.
    /// </summary>
    public sealed class FogVisibilityComponent : MonoBehaviour, IInitializable, ITickable, ISlowTickable, ILateDisposable,
        IFogVisibilityFacade, IRespawnable, IDespawnable
    {
        private const float SHIP_CHECK_INTERVAL = 0.1f;
        private const int SHIP_PHASE_COUNT = 10;
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;
        private ISlowTickService _slowTick;
        private CombatModifiers _modifiers;
        private IReadOnlyList<IVisionFootprintFacade> _footprints;
        private long _entityId;
        private float _shipCheckElapsed;

        [SerializeField] private Renderer[] renderers;
        [Tooltip("XZ footprint radius: the unit is revealed when vision reaches any part of it.")]
        [SerializeField, Min(0f)] private float revealRadius;
        [Tooltip("Once seen, stay drawn for the rest of the battle (static buildings).")]
        [SerializeField] private bool staysRevealedOnceSeen;

        private bool _isHidden;
        private bool _isReleased;

        public event Action<bool> VisibilityChanged;

        public bool IsHiddenByFog => _isHidden;

        [Inject]
        private void Construct(IVisionService visionService, ILocalPlayer localPlayer,
            CombatModifiers modifiers,
            ISlowTickService slowTick, IReadOnlyList<IVisionFootprintFacade> footprints, long entityId)
        {
            _slowTick = slowTick;
            _visionService = visionService;
            _localPlayer = localPlayer;
            _modifiers = modifiers;
            _footprints = footprints;
            _entityId = entityId;
        }

        public void Initialize()
        {
            Subscribe();
            _isHidden = IsCoveredByFog();
            ApplyVisibility();
        }

        public void Respawn()
        {
            _isReleased = false;
            Subscribe();
            _isHidden = IsCoveredByFog();
            ApplyVisibility();
        }

        public void Despawn() => LateDispose();

        private void Subscribe()
        {
            _modifiers.CloakChanged += RefreshVisibility;
            _visionService.RevealChanged += OnRevealChanged;
            if (_footprints.Count > 0)
                _shipCheckElapsed = (ulong)_entityId % SHIP_PHASE_COUNT * (SHIP_CHECK_INTERVAL / SHIP_PHASE_COUNT);
            else
                _slowTick.Add(this);
        }

        public void LateDispose()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            if (_footprints.Count == 0) _slowTick.Remove(this);
            _modifiers.CloakChanged -= RefreshVisibility;
            _visionService.RevealChanged -= OnRevealChanged;
        }

        public void SlowTick() => RefreshVisibility();

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (_isReleased || _footprints.Count == 0) return;
            _shipCheckElapsed += deltaTime;
            if (_shipCheckElapsed < SHIP_CHECK_INTERVAL) return;
            // Preserve staggered phases without replaying overdue queries after a long frame.
            _shipCheckElapsed %= SHIP_CHECK_INTERVAL;
            RefreshVisibility();
        }

        private void RefreshVisibility()
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
            VisibilityChanged?.Invoke(!isHidden);
        }

        private bool IsCoveredByFog() =>
            _modifiers.IsCloaked || !(_footprints.Count > 0
                ? _visionService.IsVisible(_localPlayer.Id, _footprints[0])
                : _visionService.IsRevealed(_localPlayer.Id, _entityId) ||
                  _visionService.IsAreaVisible(_localPlayer.Id, transform.position, revealRadius));

        private void OnRevealChanged(PlayerId viewer, long entityId, bool visible)
        {
            if (_entityId == entityId && _localPlayer.IsFriendly(viewer)) RefreshVisibility();
        }

        private void ApplyVisibility()
        {
            foreach (Renderer entityRenderer in renderers)
            {
                entityRenderer.forceRenderingOff = _isHidden;
            }
        }
    }
}
