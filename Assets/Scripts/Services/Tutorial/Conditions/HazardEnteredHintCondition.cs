using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Hazards;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when a local ship enters a hazard that changes it (abilities blocked or shields down). The card names
    /// the ship ([unit]), the hazard ([hazard]) and what it does ([effects]); the marker follows the ship.
    /// </summary>
    public sealed class HazardEnteredHintCondition : IHintCondition, IHintWorldTarget, IHintTextSource
    {
        private readonly IHazardService _hazards;
        private readonly ILocalPlayer _player;
        private readonly FactionCatalog _factions;
        private IEntity _unit;
        private string _unitName;
        private string _hazardName;
        private string _effects;

        public bool IsMet => _unit != null;

        public HazardEnteredHintCondition(IHazardService hazards, ILocalPlayer player, FactionCatalog factions)
        {
            _hazards = hazards;
            _player = player;
            _factions = factions;
            _hazards.Entered += HandleEntered;
        }

        private void HandleEntered(IEntity entity, SpaceHazardData hazard)
        {
            if (_unit != null || !_player.IsLocal(entity.Owner) || !entity.TryGetFacade(out IUnitTypeFacade unit) ||
                !unit.UnitTypeId.IsShip) return;
            List<string> effects = new List<string>();
            if (hazard.DisablesShields) effects.Add("shields are down");
            if (hazard.BlocksSpecialAbilities) effects.Add("abilities are blocked");
            if (effects.Count == 0) return;
            if (hazard.BlocksReinforcements) effects.Add("reinforcements can't arrive here");
            _unit = entity;
            _unitName = _factions.GetShipFactionData(unit.UnitTypeId.ShipType).Name;
            _hazardName = hazard.DisplayName;
            _effects = string.Join(", ", effects);
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = default;
            if (_unit == null || _unit.HealthModel.IsDestroyed) return false;
            position = _unit.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
        }

        public bool TryGetText(string token, out string value)
        {
            switch (token)
            {
                case "unit": value = _unitName; break;
                case "hazard": value = _hazardName; break;
                case "effects": value = _effects; break;
                default: value = null; break;
            }
            return value != null;
        }

        public void Dispose() => _hazards.Entered -= HandleEntered;
    }
}
