using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.TeamColor
{
    /// <summary>
    /// Tells the EmpireAtWar/Ship Lit shader which team color this unit wears.
    /// The palette index goes into each renderer's shader user value, so every player's ships
    /// share the same materials and still batch together.
    /// </summary>
    public sealed class TeamColorView : MonoBehaviour
    {
        // Matches NO_TEAM_USER_VALUE in ShipLitInput.hlsl: 0 means "no owner", n means palette index n - 1.
        private const uint FIRST_TEAM_USER_VALUE = 1u;

        [SerializeField] private MeshRenderer[] meshRenderers;

        [Inject]
        private void Construct(IPlayerRoster playerRoster, PlayerId owner)
        {
            uint userValue = GetUserValue(owner, playerRoster);
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.SetShaderUserValue(userValue);
            }
        }

        public static uint GetUserValue(PlayerId owner, IPlayerRoster playerRoster)
        {
            return FIRST_TEAM_USER_VALUE + (uint)playerRoster.Get(owner).ColorIndex;
        }
    }
}
