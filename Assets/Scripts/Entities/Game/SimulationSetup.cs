using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationSetup
    {
        public FactionType FactionA { get; }
        public FactionType FactionB { get; }
        public IReadOnlyList<SimulationFleetEntry> FleetA { get; }
        public IReadOnlyList<SimulationFleetEntry> FleetB { get; }
        public float Distance { get; }
        public float TimeLimit { get; }
        public bool FastSpeed { get; }
        /// <summary>When false, hangars never launch squadrons.</summary>
        public bool UseHangars { get; }

        public SimulationSetup(FactionType factionA, FactionType factionB,
            IEnumerable<SimulationFleetEntry> fleetA, IEnumerable<SimulationFleetEntry> fleetB,
            float distance, float timeLimit, bool fastSpeed, bool useHangars = true)
        {
            FactionA = factionA; FactionB = factionB;
            FleetA = Array.AsReadOnly(fleetA.ToArray()); FleetB = Array.AsReadOnly(fleetB.ToArray());
            if (FleetA.Count == 0 || FleetB.Count == 0) throw new ArgumentException("Both fleets must contain units.");
            if (!float.IsFinite(distance) || distance <= 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (!float.IsFinite(timeLimit) || timeLimit <= 0) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            Distance = distance; TimeLimit = timeLimit; FastSpeed = fastSpeed; UseHangars = useHangars;
        }
    }
}
