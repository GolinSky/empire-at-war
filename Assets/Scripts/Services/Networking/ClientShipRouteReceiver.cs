using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: shows the host's route lines on the local player's ships.</summary>
    public sealed class ClientShipRouteReceiver : IInitializable, ILateDisposable
    {
        private readonly IEntityLocator _entities;

        public ClientShipRouteReceiver(IEntityLocator entities)
        {
            _entities = entities;
        }

        public void Initialize() => NetworkClient.RegisterHandler<ShipRouteMessage>(OnRoute);

        public void LateDispose() => NetworkClient.UnregisterHandler<ShipRouteMessage>();

        // An entity the client has not spawned yet (or already removed) is skipped.
        private void OnRoute(ShipRouteMessage message)
        {
            if (_entities.TryGetEntity(message.EntityId, out IEntity entity) &&
                entity.TryGetFacade(out IShipRouteReplicaFacade route))
                route.ShowReplicaRoute(message.Route);
        }
    }
}
