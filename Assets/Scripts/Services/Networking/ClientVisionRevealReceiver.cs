using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Vision;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class ClientVisionRevealReceiver : IInitializable, ILateDisposable
    {
        private readonly VisionService _vision;
        private readonly IEntityLocator _entities;

        public ClientVisionRevealReceiver(VisionService vision, IEntityLocator entities)
        {
            _vision = vision;
            _entities = entities;
        }

        public void Initialize()
        {
            NetworkClient.RegisterHandler<VisionRevealMessage>(OnReveal);
            _entities.EntityRemoved += OnEntityRemoved;
        }

        public void LateDispose()
        {
            NetworkClient.UnregisterHandler<VisionRevealMessage>();
            _entities.EntityRemoved -= OnEntityRemoved;
        }

        private void OnReveal(VisionRevealMessage message)
        {
            if (!message.Active || _entities.TryGetEntity(message.EntityId, out _))
                _vision.SetReplicaReveal(message.Viewer, message.EntityId, message.Active);
        }

        private void OnEntityRemoved(IEntity entity) => _vision.ForgetReveals(entity.Id);
    }
}
