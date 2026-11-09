using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Reads live game state for step prerequisites and skip rules. Nothing is cached between calls.</summary>
    public sealed class HintStates
    {
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _player;
        private readonly IReinforcementModelObserver _reinforcements;
        private readonly IEntityLocator _entities;
        private readonly ScoutUnits _scouts;

        public HintStates(ISelectionService selection, ILocalPlayer player, IReinforcementModelObserver reinforcements,
            IEntityLocator entities, ScoutUnits scouts)
        {
            _scouts = scouts;
            _entities = entities;
            _selection = selection;
            _player = player;
            _reinforcements = reinforcements;
        }

        public bool IsSatisfied(HintStateId id)
        {
            ISelectionContext selection = _selection.PlayerSelectionContext;
            switch (id)
            {
                case HintStateId.None: return true;
                case HintStateId.UnitSelected: return selection.SelectionType == SelectionType.Ship && selection.Count > 0;
                case HintStateId.StationSelected: return selection.SelectionType == SelectionType.Base;
                case HintStateId.AbilityUnitSelected: return TryGetSelectedAbility(out _);
                case HintStateId.AutoPowerOn: return IsAutoPowerOn(selection.Entities);
                case HintStateId.AbilityNeedsMove:
                    return TryGetSelectedAbility(out ShipAbilityDefinition move) && move.AutoPower == AutoPowerRole.Travel;
                case HintStateId.AbilityNeedsTarget:
                    return TryGetSelectedAbility(out ShipAbilityDefinition target) && target.RequiresEnemyTarget;
                case HintStateId.AbilityInstant:
                    return TryGetSelectedAbility(out ShipAbilityDefinition instant) && !instant.RequiresEnemyTarget;
                case HintStateId.MiningFacilityReady: return HasMiningFacilityInReserve() || HasPlacedMiningFacility();
                case HintStateId.ScoutReady: return HasScoutInReserve();
                default: throw new System.ArgumentOutOfRangeException(nameof(id), id, "No hint state for this id.");
            }
        }

        /// <summary>Name of the ability the ability bar marks: the first slot of the first selected unit with abilities.</summary>
        public bool TryGetSelectedAbilityName(out string name)
        {
            bool found = TryGetSelectedAbility(out ShipAbilityDefinition ability);
            name = found ? ability.DisplayName : null;
            return found;
        }

        private bool TryGetSelectedAbility(out ShipAbilityDefinition ability)
        {
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (_player.IsLocal(entity.Owner) && entity.TryGetFacade(out IShipAbilityFacade abilities) &&
                    abilities.Slots.Count > 0)
                {
                    ShipAbilitySlot slot = abilities.Slots[0];
                    ability = slot.Definition;
                    return true;
                }
            ability = null;
            return false;
        }

        private bool HasMiningFacilityInReserve()
        {
            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve)
                if (entry.Key is MiningFacilityUnitRequest && entry.Value > 0) return true;
            return false;
        }

        private bool HasScoutInReserve()
        {
            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve)
                if (entry.Value > 0 && _scouts.IsScout(entry.Key)) return true;
            return false;
        }

        private bool HasPlacedMiningFacility()
        {
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IMiningFacilityFacade _))
                    return true;
            return false;
        }

        private bool IsAutoPowerOn(IReadOnlyList<IEntity> entities)
        {
            foreach (IEntity entity in entities)
                if (_player.IsLocal(entity.Owner) && entity.TryGetFacade(out IShipAutoPowerFacade autoPower) &&
                    autoPower.Eligible && autoPower.Enabled)
                    return true;
            return false;
        }
    }
}
