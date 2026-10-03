using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Uploads the team palette that every unit using EmpireAtWar/Ship Lit reads.</summary>
    public sealed class TeamColorService : IInitializable
    {
        private readonly TeamColorPalette _palette;

        private static readonly int TEAM_COLORS_ID = Shader.PropertyToID("_TeamColors");

        public TeamColorService(TeamColorPalette palette)
        {
            _palette = palette;
        }

        public void Initialize()
        {
            Shader.SetGlobalVectorArray(TEAM_COLORS_ID, _palette.CreateLinearColors());
        }
    }
}
