using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>
    /// Every local ownership change of a capture site: captured, taken by the enemy, or lost back to neutral
    /// (facility destroyed), plus finished local construction.
    /// </summary>
    public sealed class CaptureSiteNotifier : IInitializable, ILateDisposable
    {
        private readonly IBattleNotifications _notifications;
        private readonly ICaptureSiteEvents _sites;
        private readonly CaptureSiteData _data;
        private readonly ILocalPlayer _local;

        public CaptureSiteNotifier(IBattleNotifications notifications, ICaptureSiteEvents sites, CaptureSiteData data,
            ILocalPlayer local)
        {
            _notifications = notifications;
            _sites = sites;
            _data = data;
            _local = local;
        }

        public void Initialize()
        {
            _sites.SiteCaptured += HandleCaptured;
            _sites.SiteLost += HandleLost;
            _sites.ConstructionCompleted += HandleConstructionCompleted;
        }

        public void LateDispose()
        {
            _sites.SiteCaptured -= HandleCaptured;
            _sites.SiteLost -= HandleLost;
            _sites.ConstructionCompleted -= HandleConstructionCompleted;
        }

        private void HandleCaptured(PlayerId previous, PlayerId owner, SiteFacilityType type)
        {
            if (_local.IsLocal(owner))
                _notifications.Post(BattleNotificationPriority.Tactical, $"{Name(type)} captured");
            else if (_local.IsLocal(previous) && _local.IsHostile(owner))
                _notifications.Post(BattleNotificationPriority.Urgent, $"{Name(type)} taken by the enemy");
        }

        private void HandleLost(PlayerId owner, SiteFacilityType type)
        {
            if (_local.IsLocal(owner))
                _notifications.Post(BattleNotificationPriority.Urgent, $"{Name(type)} destroyed");
        }

        private void HandleConstructionCompleted(PlayerId owner, SiteFacilityType type)
        {
            if (_local.IsLocal(owner))
                _notifications.Post(BattleNotificationPriority.Progress, $"{Name(type)} construction complete",
                    "site.built", "{0} facilities completed");
        }

        private string Name(SiteFacilityType type) => _data.GetCost(type).Name;
    }
}
