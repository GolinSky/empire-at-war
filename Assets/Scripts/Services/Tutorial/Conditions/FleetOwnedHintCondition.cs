using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met once the player has a fleet: <see cref="FLEET_SIZE"/> living ships.</summary>
    public sealed class FleetOwnedHintCondition : IPolledHintCondition
    {
        public const int FLEET_SIZE = 5;
        private const float CHECK_SECONDS = 0.5f;

        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private float _sinceCheck = CHECK_SECONDS;

        public bool IsMet { get; private set; }

        public FleetOwnedHintCondition(IEntityLocator entities, ILocalPlayer player)
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
            int ships = 0;
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed && entity.IsShip())
                    ships++;
            IsMet = ships >= FLEET_SIZE;
        }

        public void Dispose()
        {
        }
    }
}
