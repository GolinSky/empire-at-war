using EmpireAtWar.Models.Players;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.TeamColor
{
    /// <summary>
    /// Composes team color, highlight flags and hull damage without changing shared materials.
    /// Bit layout matches ShipLitInput.hlsl.
    /// </summary>
    public sealed class UnitRenderStateComponent : MonoBehaviour, IUnitRenderStateComponent
    {
        // Matches NO_TEAM_USER_VALUE in ShipLitInput.hlsl: 0 means "no owner", n means palette index n - 1.
        private const uint FIRST_TEAM_USER_VALUE = 1u;
        private const uint TEAM_USER_VALUE_MASK = 0xFFu;
        private const int HIGHLIGHT_SHIFT = 8;
        private const uint HIGHLIGHT_MASK = 0x7u << HIGHLIGHT_SHIFT;
        private const int HULL_DAMAGE_LEVEL_SHIFT = 11;
        private const uint HULL_DAMAGE_LEVEL_MASK = 0xFu << HULL_DAMAGE_LEVEL_SHIFT;
        private const int HULL_DAMAGE_MAX_LEVEL = 15;
        private const int HULL_DAMAGE_SEED_SHIFT = 15;
        private const uint HULL_DAMAGE_SEED_MASK = 0x1Fu << HULL_DAMAGE_SEED_SHIFT;

        [SerializeField] private MeshRenderer[] meshRenderers;

        private uint _userValue;

        /// <summary>Team, hull damage and pattern seed, without the live highlight: what a wreck inherits.</summary>
        public uint WreckUserValue => _userValue & ~HIGHLIGHT_MASK;

        [Inject]
        private void Construct(IPlayerRoster playerRoster, PlayerId owner)
        {
            ApplyUserValue(GetTeamUserValue(owner, playerRoster) | GetSeedBits() |
                (_userValue & (HIGHLIGHT_MASK | HULL_DAMAGE_LEVEL_MASK)));
        }

        public void SetHighlight(UnitHighlight flags)
        {
            ApplyUserValue((_userValue & ~HIGHLIGHT_MASK) | ((uint)flags << HIGHLIGHT_SHIFT));
        }

        /// <summary>Quantizes hull (1 = intact) into 16 damage levels; any lost hull shows at least level 1.</summary>
        public void SetHullDamage(float hullPercentage)
        {
            uint level = (uint)Mathf.Clamp(Mathf.CeilToInt((1f - hullPercentage) * HULL_DAMAGE_MAX_LEVEL), 0,
                HULL_DAMAGE_MAX_LEVEL);
            ApplyUserValue((_userValue & ~HULL_DAMAGE_LEVEL_MASK) | (level << HULL_DAMAGE_LEVEL_SHIFT));
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
                uint outlineLayers = (userValue & HIGHLIGHT_MASK) != 0
                    ? layers | outlineLayer
                    : layers & ~outlineLayer;
                if (layers != outlineLayers)
                    meshRenderer.renderingLayerMask = outlineLayers;
            }
        }

        // Stable per unit, no shared random state: ships of one class get different scorch patterns.
        private uint GetSeedBits()
        {
            uint seed = unchecked((uint)GetEntityId().GetHashCode() * 2654435761u) >> 27;
            return seed << HULL_DAMAGE_SEED_SHIFT;
        }

        private static uint GetTeamUserValue(PlayerId owner, IPlayerRoster playerRoster)
        {
            return FIRST_TEAM_USER_VALUE + (uint)playerRoster.Get(owner).ColorIndex;
        }
    }
}
