using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the local player has a mining facility placed on the map.</summary>
    public sealed class MiningFacilityPlacedHintCondition : IPolledHintCondition
    {
        private const float CHECK_SECONDS = 0.5f;

        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private float _sinceCheck = CHECK_SECONDS;

        public bool IsMet { get; private set; }

        public MiningFacilityPlacedHintCondition(IEntityLocator entities, ILocalPlayer player)
        {
            _entities = entities;
            _player = player;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            _sinceCheck += deltaTime;
            if (_sinceCheck < CHECK_SECONDS) return;
            _sinceCheck = 0f;
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IMiningFacilityFacade _))
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
