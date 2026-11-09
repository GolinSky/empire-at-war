using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met while the local player owns any living unit that can move (ship or squadron).</summary>
    public sealed class LocalUnitOwnedHintCondition : IPolledHintCondition
    {
        private const float CHECK_SECONDS = 0.5f;

        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private float _sinceCheck = CHECK_SECONDS;

        public bool IsMet { get; private set; }

        public LocalUnitOwnedHintCondition(IEntityLocator entities, ILocalPlayer player)
        {
            _entities = entities;
            _player = player;
        }

        public void Poll(float deltaTime)
        {
            _sinceCheck += deltaTime;
            if (_sinceCheck < CHECK_SECONDS) return;
            _sinceCheck = 0f;
            IsMet = false;
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IMoveFacade _))
                {
                    IsMet = true;
                    return;
                }
        }

        public void Dispose()
        {
        }
    }
}
