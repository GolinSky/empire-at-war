using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class ClientFocusFireReceiver : IInitializable, ILateDisposable
    {
        private readonly ReplicatedFocusFireCounts _counts;
        private readonly IEntityLocator _entities;

        public ClientFocusFireReceiver(ReplicatedFocusFireCounts counts, IEntityLocator entities)
        {
            _counts = counts;
            _entities = entities;
        }

        public void Initialize()
        {
            NetworkClient.RegisterHandler<FocusFireCountMessage>(Receive);
            NetworkClient.OnDisconnectedEvent += _counts.Clear;
            _entities.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            NetworkClient.UnregisterHandler<FocusFireCountMessage>();
            NetworkClient.OnDisconnectedEvent -= _counts.Clear;
            _entities.EntityRemoved -= HandleEntityRemoved;
            _counts.Clear();
        }

        private void Receive(FocusFireCountMessage message) =>
            _counts.Apply(message.TargetId, message.Ships, message.Squadrons);

        private void HandleEntityRemoved(IEntity entity) => _counts.Remove(entity.Id);
    }
}
