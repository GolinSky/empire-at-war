using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Names the reason when a local unit's ability request or target is rejected.</summary>
    public sealed class AbilityTargetingNotifier : IInitializable, ILateDisposable
    {
        private readonly IBattleNotifications _notifications;
        private readonly ShipAbilityService _abilities;
        private readonly ILocalPlayer _local;

        public AbilityTargetingNotifier(IBattleNotifications notifications, ShipAbilityService abilities,
            ILocalPlayer local)
        {
            _notifications = notifications;
            _abilities = abilities;
            _local = local;
        }

        public void Initialize() => _abilities.TargetRejected += HandleRejected;

        public void LateDispose() => _abilities.TargetRejected -= HandleRejected;

        private void HandleRejected(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity caster in casters)
            {
                if (!_local.IsLocal(caster.Owner)) continue;
                // A submitted target keeps targeting open; a pressed ability that cannot start has no target.
                if (_abilities.IsWaitingForTarget)
                    _notifications.Post(BattleNotificationPriority.Feedback, "Invalid target", "invalid-target");
                else
                    _notifications.Post(BattleNotificationPriority.Feedback, "Ability unavailable", "ability-unavailable");
                return;
            }
        }
    }
}
