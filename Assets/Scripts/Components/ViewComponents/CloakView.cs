using EmpireAtWar.Components.Combat;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents
{
    public sealed class CloakView : MonoBehaviour
    {
        [SerializeField] private Renderer hullRenderer;
        [SerializeField] private Renderer shieldRenderer;
        [SerializeField] private Material cloakMaterial;

        private CombatModifiers _modifiers;
        private Material _visibleMaterial;
        private bool _isInitialized;
        private bool _isCloaked;

        [Inject]
        private void Construct(CombatModifiers modifiers, ILocalPlayer localPlayer, PlayerId owner)
        {
            // Enemy hulls and shields are hidden completely by FogVisibilityComponent.
            if (!localPlayer.IsFriendly(owner)) return;

            _modifiers = modifiers;
            _visibleMaterial = hullRenderer.sharedMaterial;
            _modifiers.Changed += Refresh;
            _isInitialized = true;
            Refresh();
        }

        private void Refresh()
        {
            if (_isCloaked == _modifiers.IsCloaked) return;

            _isCloaked = _modifiers.IsCloaked;
            hullRenderer.sharedMaterial = _isCloaked ? cloakMaterial : _visibleMaterial;
            shieldRenderer.forceRenderingOff = _isCloaked;
        }

        private void OnDestroy()
        {
            if (_isInitialized) _modifiers.Changed -= Refresh;
        }
    }
}
