using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.ShipAbilities;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Client: gives local units the host's attack targets and plays the host's abilities on them. Facades are
    /// called directly, not through the order service, so no local order feedback or highlights appear.
    /// Movement from these orders is overwritten by replication; damage is ignored on clients.
    /// </summary>
    public sealed class ClientCombatEventReceiver : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly ShipAbilityService _abilities;

        public ClientCombatEventReceiver(IEntityLocator entities, ShipAbilityService abilities)
        {
            _entities = entities;
            _abilities = abilities;
        }

        public void Initialize()
        {
            NetworkClient.RegisterHandler<AttackTargetMessage>(OnAttack);
            NetworkClient.RegisterHandler<ShipAbilityEventMessage>(OnAbility);
            NetworkClient.RegisterHandler<WeaponTargetMessage>(OnWeaponTarget);
        }

        public void LateDispose()
        {
            NetworkClient.UnregisterHandler<AttackTargetMessage>();
            NetworkClient.UnregisterHandler<ShipAbilityEventMessage>();
            NetworkClient.UnregisterHandler<WeaponTargetMessage>();
        }

        // An entity the client has not spawned yet (or already removed) is skipped.
        private void OnAttack(AttackTargetMessage message)
        {
            if (!_entities.TryGetEntity(message.TargetId, out IEntity target)) return;
            bool targetsHardPoint = message.HardPointId != UnitOrderModel.NO_HARD_POINT;
            foreach (long receiverId in message.ReceiverIds)
            {
                if (!_entities.TryGetEntity(receiverId, out IEntity receiver)) continue;
                if (targetsHardPoint && receiver.TryGetFacade(out IHardPointAttackFacade hardPointAttack))
                    hardPointAttack.AttackHardPoint(target, message.HardPointId, Vector3.zero);
                else if (receiver.TryGetFacade(out IAttackFacade attack))
                    attack.Attack(target, Vector3.zero);
                else if (receiver.TryGetFacade(out IFocusFireFacade focus))
                    focus.FocusFire(target);
            }
        }

        // Client weapons ignore their own picks and engage what the host's weapons engage.
        private void OnWeaponTarget(WeaponTargetMessage message)
        {
            if (!_entities.TryGetEntity(message.ShooterId, out IEntity shooter) ||
                !shooter.TryGetFacade(out IWeaponTargetReplicaFacade weapon)) return;
            if (message.Reset)
            {
                weapon.ResetTarget();
                return;
            }

            if (_entities.TryGetEntity(message.TargetId, out IEntity target))
                weapon.AddTarget(target, message.AttackType, message.HardPointId, message.HardPointType);
        }

        private void OnAbility(ShipAbilityEventMessage message)
        {
            if (!_entities.TryGetEntity(message.CasterId, out IEntity casterEntity) ||
                !casterEntity.TryGetFacade(out IShipAbilityFacade caster)) return;
            if (message.Canceled)
            {
                _abilities.CancelReplica(caster, message.Ability);
                return;
            }

            IEntity target = null;
            if (message.TargetId != 0 && !_entities.TryGetEntity(message.TargetId, out target)) return;
            _abilities.ActivateReplica(caster, message.Ability, target);
        }
    }
}
