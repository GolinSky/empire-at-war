using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>Online lobby: a member's avatar; false while it is not available (yet).</summary>
    public interface ISkirmishAvatarSource
    {
        bool TryGetAvatar(ulong steamId, out Texture2D avatar);
    }
}
