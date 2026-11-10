using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Build-then-deploy progress for ships (ships and squadrons) or mining facilities: all built and waiting in
    /// reinforcements, or all deployed.
    /// </summary>
    public sealed class ReserveHintCondition : IPolledHintCondition
    {
        public enum Kind
        {
            ShipsBuilt,
            ShipsDeployed,
            FacilitiesBuilt,
            FacilitiesDeployed
        }

        private readonly IPlayerFactionModelObserver _faction;
        private readonly IReinforcementModelObserver _reinforcements;
        private readonly Kind _kind;

        public bool IsMet { get; private set; }

        public ReserveHintCondition(IPlayerFactionModelObserver faction, IReinforcementModelObserver reinforcements,
            Kind kind)
        {
            _faction = faction;
            _reinforcements = reinforcements;
            _kind = kind;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            bool isBuilt = _kind == Kind.ShipsBuilt || _kind == Kind.FacilitiesBuilt;
            IsMet = isBuilt ? !IsInProduction() && IsInReserve() : !IsInReserve();
        }

        private bool IsWanted(UnitRequest request) =>
            _kind == Kind.ShipsBuilt || _kind == Kind.ShipsDeployed
                ? request is ShipUnitRequest || request is SquadronUnitRequest
                : request is MiningFacilityUnitRequest;

        private bool IsInProduction()
        {
            foreach (ProductionQueueSnapshot item in _faction.GetProductionQueueSnapshots())
                if (IsWanted(item.UnitRequest)) return true;
            return false;
        }

        private bool IsInReserve()
        {
            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve)
                if (entry.Value > 0 && IsWanted(entry.Key)) return true;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
