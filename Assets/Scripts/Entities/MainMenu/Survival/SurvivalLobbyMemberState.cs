using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    /// <summary>Online Survival lobby: one commander chip (name, host/ready state, Steam avatar or null).</summary>
    public readonly struct SurvivalLobbyMemberState
    {
        public readonly string Name;
        public readonly string Status;
        public readonly bool IsReady;
        public readonly Texture2D Avatar;

        public SurvivalLobbyMemberState(string name, string status, bool isReady, Texture2D avatar)
        {
            Name = name;
            Status = status;
            IsReady = isReady;
            Avatar = avatar;
        }
    }
}
