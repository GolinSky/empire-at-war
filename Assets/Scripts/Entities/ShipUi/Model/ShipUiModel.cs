using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ShipUi
{
    public class ShipUiModel : Model, IShipUiModelObserver
    {
        public event Action OnSelectionChanged;

        public bool HasShips { get; private set; }
        public ShipType? SelectedShipType { get; private set; }
        public SquadronType? SelectedSquadronType { get; private set; }
        public ShipAbilityId? PendingAbilityId { get; private set; }
        private IReadOnlyList<IEntity> _pendingCasters;

        public void SetPendingAbility(ShipAbilityId? id,
            IReadOnlyList<IEntity> casters)
        {
            PendingAbilityId = id;
            _pendingCasters = casters;
        }

        public bool IsPendingCaster(IEntity entity)
        {
            if (!PendingAbilityId.HasValue) return false;
            for (int i = 0; i < _pendingCasters.Count; i++)
                if (_pendingCasters[i] == entity) return true;
            return false;
        }

        public void UpdateSelection(bool hasShips, ShipType? selectedShipType,
            SquadronType? selectedSquadronType = null)
        {
            HasShips = hasShips;
            SelectedShipType = selectedShipType;
            SelectedSquadronType = selectedSquadronType;
            OnSelectionChanged?.Invoke();
        }
    }
}
