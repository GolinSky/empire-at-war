using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    /// <summary>The on-screen color of an owner: its team palette color, or neutral grey for no owner.</summary>
    public interface IPlayerColors
    {
        Color GetColor(PlayerId owner);
    }
}
