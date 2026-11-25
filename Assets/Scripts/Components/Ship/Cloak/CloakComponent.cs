using EmpireAtWar.Components.Combat;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Ship.Cloak
{
    /// <summary>
    /// How the owner and allies see a cloaked ship: the hull switches to the cloak material and the shield hides.
    /// <see cref="EmpireAtWar.Ship.ShipInstaller"/> registers it only for friendly ships; hostile viewers see
    /// nothing, because FogVisibilityComponent hides the whole ship while it is cloaked.
    /// </summary>
    public sealed class CloakComponent : MonoBehaviour, IMonoComponent, IInitializable
    {
        [SerializeField] private Renderer[] hullRenderers;
        [SerializeField] private Renderer shieldRenderer;
        [SerializeField] private Material cloakMaterial;

        private CombatModifiers _modifiers;
        private Material[] _visibleMaterials;
        private bool _isCloaked;
        private bool _isReleased = true;

        [Inject]
        private void Construct(CombatModifiers modifiers)
        {
            _modifiers = modifiers;
        }

        public void Initialize()
        {
            _isReleased = false;
            _visibleMaterials = new Material[hullRenderers.Length];
            for (int i = 0; i < hullRenderers.Length; i++) _visibleMaterials[i] = hullRenderers[i].sharedMaterial;
            _modifiers.Changed += Refresh;
            Refresh();
        }

        public void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            _modifiers.Changed -= Refresh;
            // A reused ship view starts its next life uncloaked.
            Apply(false);
        }

        private void Refresh() => Apply(_modifiers.IsCloaked);

        private void Apply(bool isCloaked)
        {
            if (_isCloaked == isCloaked) return;
            _isCloaked = isCloaked;
            for (int i = 0; i < hullRenderers.Length; i++)
                hullRenderers[i].sharedMaterial = isCloaked ? cloakMaterial : _visibleMaterials[i];
            shieldRenderer.forceRenderingOff = isCloaked;
        }
    }
}
