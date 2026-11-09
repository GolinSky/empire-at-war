using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Services.ReinforcementZones;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>
    /// Map cards: own reinforcement zones gained or lost, and a selected or high-value local ship entering a
    /// hazard that changes it.
    /// </summary>
    public sealed class MapNotifier : IInitializable, ILateDisposable
    {
        private readonly IBattleNotifications _notifications;
        private readonly IReinforcementZonesSystem _zones;
        private readonly IHazardService _hazards;
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _local;
        private readonly FactionCatalog _factions;
        private int _ownedZones;

        public MapNotifier(IBattleNotifications notifications, IReinforcementZonesSystem zones, IHazardService hazards,
            ISelectionService selection, ILocalPlayer local, FactionCatalog factions)
        {
            _notifications = notifications;
            _zones = zones;
            _hazards = hazards;
            _selection = selection;
            _local = local;
            _factions = factions;
        }

        public void Initialize()
        {
            _ownedZones = _zones.GetOwnedCapturableZoneCount(_local.Id);
            _zones.OwnershipChanged += HandleOwnershipChanged;
            _hazards.Entered += HandleHazardEntered;
        }

        public void LateDispose()
        {
            _zones.OwnershipChanged -= HandleOwnershipChanged;
            _hazards.Entered -= HandleHazardEntered;
        }

        private void HandleOwnershipChanged()
        {
            int owned = _zones.GetOwnedCapturableZoneCount(_local.Id);
            if (owned < _ownedZones)
                _notifications.Post(BattleNotificationPriority.Tactical, "Reinforcement zone lost", "zone.lost",
                    "{0} reinforcement zones lost");
            else if (owned > _ownedZones)
                _notifications.Post(BattleNotificationPriority.Progress, "Reinforcement zone captured", "zone.captured",
                    "{0} reinforcement zones captured");
            _ownedZones = owned;
        }

        private void HandleHazardEntered(IEntity entity, SpaceHazardData hazard)
        {
            if (!_local.IsLocal(entity.Owner) || !entity.TryGetFacade(out IUnitTypeFacade unit) ||
                !unit.UnitTypeId.IsShip) return;
            string effect = Effect(hazard);
            if (effect == null) return;
            FactionData data = _factions.GetShipFactionData(unit.UnitTypeId.ShipType);
            ShipClass shipClass = entity.HealthModel.ShipClass;
            bool isHighValue = data.IsHero || shipClass == ShipClass.Capital || shipClass == ShipClass.HeavyCapital;
            if (!isHighValue && !_selection.PlayerSelectionContext.Contains(entity)) return;
            _notifications.Post(BattleNotificationPriority.Tactical, $"{data.Name} entered {hazard.DisplayName} — {effect}",
                $"hazard.{hazard.DisplayName}", $"{{0}} ships entered {hazard.DisplayName} — {effect}");
        }

        private static string Effect(SpaceHazardData hazard)
        {
            var effects = new List<string>(2);
            if (hazard.DisablesShields) effects.Add("shields down");
            if (hazard.BlocksSpecialAbilities) effects.Add("abilities blocked");
            return effects.Count == 0 ? null : string.Join(", ", effects);
        }
    }
}
