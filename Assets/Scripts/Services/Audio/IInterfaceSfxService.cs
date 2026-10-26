using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IInterfaceSfxService
    {
        bool IsAnnouncing { get; }
        void PlayRejected();
        void PlayAbilityTargetingCanceled();
        void PlayAnnouncement(AudioClip clip);
        bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority);
        bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction);
    }
}
