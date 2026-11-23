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
        private const uint HULL_DAMAGE_LEVEL_MASK = 0x1Fu << HULL_DAMAGE_LEVEL_SHIFT;
        private const int HULL_DAMAGE_MAX_LEVEL = 31;
        private const int HULL_DAMAGE_SEED_SHIFT = 16;
        private const uint HULL_DAMAGE_SEED_MASK = 0x1Fu << HULL_DAMAGE_SEED_SHIFT;
        private const int OUTLINE_OWNER_SHIFT = 21;
        private const uint OUTLINE_OWNER_COUNT = 0x7FFu;

        [SerializeField] private MeshRenderer[] meshRenderers;

        private uint _userValue;

        /// <summary>Team, hull damage and pattern seed, without the live highlight: what a wreck inherits.</summary>
        public uint WreckUserValue => _userValue & ~HIGHLIGHT_MASK;

        [Inject]
        private void Construct(IPlayerRoster playerRoster, PlayerId owner)
        {
            ApplyUserValue(GetTeamUserValue(owner, playerRoster) | GetSeedBits() | GetOutlineOwnerBits() |
                (_userValue & (HIGHLIGHT_MASK | HULL_DAMAGE_LEVEL_MASK)));
        }

        public void SetHighlight(UnitHighlight flags)
        {
            ApplyUserValue((_userValue & ~HIGHLIGHT_MASK) | ((uint)flags << HIGHLIGHT_SHIFT));
        }

        /// <summary>Quantizes hull (1 = intact) into 32 damage levels (0-31); any lost hull shows at least level 1.</summary>
        public void SetHullDamage(float hullPercentage)
        {
            uint level = (uint)GetHullDamageLevel(hullPercentage);
            ApplyUserValue((_userValue & ~HULL_DAMAGE_LEVEL_MASK) | (level << HULL_DAMAGE_LEVEL_SHIFT));
        }

        /// <summary>Hull scorch level 0-31 shown for this hull; each higher level switches on more marks.</summary>
        public static int GetHullDamageLevel(float hullPercentage) =>
            Mathf.Clamp(Mathf.CeilToInt((1f - hullPercentage) * HULL_DAMAGE_MAX_LEVEL), 0, HULL_DAMAGE_MAX_LEVEL);

        // The entity factory writes team and damage while the unit is still inactive. The GPU Resident Drawer
        // registers renderers on activation into reused instance slots, so a value written before that can lose to
        // the slot's previous (dead, damaged) unit. Writing again once enabled keeps the drawer in sync.
        private void OnEnable()
        {
            WriteUserValue();
        }

        private void ApplyUserValue(uint userValue)
        {
            if (_userValue == userValue)
            {
                return;
            }

            _userValue = userValue;
            WriteUserValue();
        }

        private void WriteUserValue()
        {
            uint userValue = _userValue;
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

        // 1-2047, shared by all of this unit's renderers: an outline hides only over its own unit's pixels.
        private uint GetOutlineOwnerBits()
        {
            uint owner = 1u + unchecked((uint)GetEntityId().GetHashCode() * 2246822519u) % OUTLINE_OWNER_COUNT;
            return owner << OUTLINE_OWNER_SHIFT;
        }

        private static uint GetTeamUserValue(PlayerId owner, IPlayerRoster playerRoster)
        {
            return FIRST_TEAM_USER_VALUE + (uint)playerRoster.Get(owner).ColorIndex;
        }
    }
}
