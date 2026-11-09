using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the local player owns a hero, or with <c>squadrons</c>, a squadron.</summary>
    public sealed class LocalUnitAddedHintCondition : IHintCondition
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly FactionCatalog _factions;
        private readonly bool _squadrons;

        public bool IsMet { get; private set; }

        public LocalUnitAddedHintCondition(IEntityLocator entities, ILocalPlayer player, FactionCatalog factions,
            bool squadrons)
        {
            _entities = entities;
            _player = player;
            _factions = factions;
            _squadrons = squadrons;
            _entities.EntityAdded += HandleAdded;
            foreach (IEntity entity in _entities.Entities) HandleAdded(entity);
        }

        private void HandleAdded(IEntity entity)
        {
            if (IsMet || !_player.IsLocal(entity.Owner) || !entity.TryGetFacade(out IUnitTypeFacade unit)) return;
            UnitTypeId type = unit.UnitTypeId;
            if (_squadrons)
            {
                if (!type.IsShip) IsMet = true;
                return;
            }
            FactionData data = type.IsShip
                ? _factions.GetShipFactionData(type.ShipType)
                : _factions.GetSquadronFactionData(type.SquadronType);
            if (data.IsHero) IsMet = true;
        }

        public void Dispose() => _entities.EntityAdded -= HandleAdded;
    }
}
