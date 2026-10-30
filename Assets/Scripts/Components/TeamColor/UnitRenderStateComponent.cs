using EmpireAtWar.Models.Players;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.TeamColor
{
    /// <summary>
    /// Composes team color and highlight flags without changing shared materials.
    /// </summary>
    public sealed class UnitRenderStateComponent : MonoBehaviour, IUnitRenderStateComponent
    {
        // Matches NO_TEAM_USER_VALUE in ShipLitInput.hlsl: 0 means "no owner", n means palette index n - 1.
        private const uint FIRST_TEAM_USER_VALUE = 1u;
        private const uint TEAM_USER_VALUE_MASK = 0xFFu;
        private const int HIGHLIGHT_SHIFT = 8;

        [SerializeField] private MeshRenderer[] meshRenderers;

        private uint _userValue;

        [Inject]
        private void Construct(IPlayerRoster playerRoster, PlayerId owner)
        {
            ApplyUserValue(GetTeamUserValue(owner, playerRoster) | (_userValue & ~TEAM_USER_VALUE_MASK));
        }

        public void SetHighlight(UnitHighlight flags)
        {
            ApplyUserValue((_userValue & TEAM_USER_VALUE_MASK) | ((uint)flags << HIGHLIGHT_SHIFT));
        }

        private void ApplyUserValue(uint userValue)
        {
            if (_userValue == userValue)
            {
                return;
            }

            _userValue = userValue;
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.SetShaderUserValue(userValue);
                uint layers = meshRenderer.renderingLayerMask;
                uint outlineLayer = UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER;
                uint outlineLayers = (userValue >> HIGHLIGHT_SHIFT) != 0
                    ? layers | outlineLayer
                    : layers & ~outlineLayer;
                if (layers != outlineLayers)
                    meshRenderer.renderingLayerMask = outlineLayers;
            }
        }

        public static uint GetTeamUserValue(PlayerId owner, IPlayerRoster playerRoster)
        {
            return FIRST_TEAM_USER_VALUE + (uint)playerRoster.Get(owner).ColorIndex;
        }
    }
}
