using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class DirectCombatAiState : IEnemyAiStateProvider
    {
        private readonly IEntityLocator _entities;
        private readonly PlayerSlot _owner;
        public EnemyStrategicState CurrentState => EnemyStrategicState.AssaultBase;
        public int ActiveShipCount => _entities.Entities.Count(entity =>
            entity.Owner == _owner.Id && !entity.HealthModel.IsDestroyed);
        public DirectCombatAiState(IEntityLocator entities, PlayerSlot owner) { _entities = entities; _owner = owner; }
    }
}
