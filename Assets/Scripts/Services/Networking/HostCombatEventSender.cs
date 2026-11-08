using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: relays attack targets and ability starts/cancels (players and AI) so clients aim their weapons and
    /// play abilities like the host. Damage stays on the host.
    /// </summary>
    public sealed class HostCombatEventSender : IInitializable, ILateDisposable
    {
        private const long NO_TARGET = 0;

        private readonly UnitOrderService _orders;
        private readonly ShipAbilityService _abilities;
        private readonly OnlineMatchService _online;

        public HostCombatEventSender(UnitOrderService orders, ShipAbilityService abilities, OnlineMatchService online)
        {
            _orders = orders;
            _abilities = abilities;
            _online = online;
        }

        public void Initialize()
        {
            _orders.AttackIssued += OnAttack;
            _abilities.Activated += OnActivated;
            _abilities.Canceled += OnCanceled;
        }

        public void LateDispose()
        {
            _orders.AttackIssued -= OnAttack;
            _abilities.Activated -= OnActivated;
            _abilities.Canceled -= OnCanceled;
        }

        private void OnAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId)
        {
            if (!_online.HasBegun) return;
            long[] ids = new long[receivers.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = receivers[i].Id;
            HostBroadcast.ToClients(_online, new AttackTargetMessage { ReceiverIds = ids, TargetId = target.Id, HardPointId = hardPointId });
        }

        private void OnActivated(IShipAbilityFacade caster, ShipAbilityId id, IEntity target)
        {
            if (!_online.HasBegun) return;
            HostBroadcast.ToClients(_online, new ShipAbilityEventMessage
            {
                CasterId = caster.Entity.Id,
                Ability = id,
                TargetId = target == null ? NO_TARGET : target.Id
            });
        }

        private void OnCanceled(IShipAbilityFacade caster, ShipAbilityId id)
        {
            if (!_online.HasBegun) return;
            HostBroadcast.ToClients(_online, new ShipAbilityEventMessage { CasterId = caster.Entity.Id, Ability = id, Canceled = true });
        }
    }
}
