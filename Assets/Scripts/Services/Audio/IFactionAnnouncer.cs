using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    /// <summary>Plays the local faction's announcer lines, sharing one cooldown per clip.</summary>
    public interface IFactionAnnouncer
    {
        FactionAnnouncerSet Clips { get; }

        bool Announce(AudioClip clip, AnnouncementPriority priority, float cooldown);
    }
}
