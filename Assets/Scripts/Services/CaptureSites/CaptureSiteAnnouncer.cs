using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class CaptureSiteAnnouncer : IInitializable, ILateDisposable
    {
        private readonly ICaptureSiteEvents _sites;
        private readonly ILocalPlayer _local;
        private readonly IFactionAnnouncer _announcer;
        private float _captureReadyAt;

        public CaptureSiteAnnouncer(ICaptureSiteEvents sites, ILocalPlayer local, IFactionAnnouncer announcer)
        {
            _sites = sites;
            _local = local;
            _announcer = announcer;
        }

        public void Initialize()
        {
            _sites.SiteCaptured += HandleCaptured;
            _sites.SiteLost += HandleLost;
            _sites.ConstructionStarted += HandleConstructionStarted;
            _sites.ConstructionCompleted += HandleConstructionCompleted;
        }

        public void LateDispose()
        {
            _sites.SiteCaptured -= HandleCaptured;
            _sites.SiteLost -= HandleLost;
            _sites.ConstructionStarted -= HandleConstructionStarted;
            _sites.ConstructionCompleted -= HandleConstructionCompleted;
        }

        private void HandleConstructionStarted(PlayerId payer, SiteFacilityType type)
        {
            if (payer == _local.Id)
                _announcer.Announce(_announcer.Clips.BuildingStarted, AnnouncementPriority.Progress,
                    AnnouncementCooldowns.EVENT);
        }

        private void HandleConstructionCompleted(PlayerId owner, SiteFacilityType type)
        {
            if (owner == _local.Id && SiteFacilityBuilder.IsSatellite(type))
                _announcer.Announce(_announcer.Clips.ConstructionComplete, AnnouncementPriority.Progress,
                    AnnouncementCooldowns.CONSTRUCTION);
        }

        private void HandleCaptured(PlayerId previous, PlayerId owner, SiteFacilityType type)
        {
            if (type != SiteFacilityType.Mining || owner != _local.Id || !previous.IsNone ||
                Time.unscaledTime < _captureReadyAt) return;
            if (_announcer.Announce(_announcer.Clips.MiningCaptured, AnnouncementPriority.Notification,
                    AnnouncementCooldowns.EVENT))
                _captureReadyAt = Time.unscaledTime + AnnouncementCooldowns.EVENT;
        }

        private void HandleLost(PlayerId owner, SiteFacilityType type)
        {
            if (owner != _local.Id) return;
            if (type == SiteFacilityType.Mining)
                _announcer.Announce(_announcer.Clips.MiningDestroyed, AnnouncementPriority.Warning,
                    AnnouncementCooldowns.EVENT);
            else if (SiteFacilityBuilder.IsSatellite(type))
                _announcer.Announce(_announcer.Clips.UnitDestroyed, AnnouncementPriority.Notification,
                    AnnouncementCooldowns.LOSS);
        }
    }
}
