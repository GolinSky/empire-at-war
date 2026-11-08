using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: gives local units the host's current orders.</summary>
    public sealed class ClientUnitOrderReceiver : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;

        public ClientUnitOrderReceiver(IEntityLocator entities)
        {
            _entities = entities;
        }

        public void Initialize() => NetworkClient.RegisterHandler<UnitOrderStateMessage>(OnOrder);

        public void LateDispose() => NetworkClient.UnregisterHandler<UnitOrderStateMessage>();

        // An entity the client has not spawned yet (or already removed) is skipped.
        private void OnOrder(UnitOrderStateMessage message)
        {
            if (_entities.TryGetEntity(message.EntityId, out IEntity entity) &&
                entity.TryGetFacade(out IUnitOrderReplicaFacade orders))
                orders.ApplyReplicaOrder(message.Order, message.IsNewOrder);
        }
    }
}
