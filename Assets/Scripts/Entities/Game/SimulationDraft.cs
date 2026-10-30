using System.Collections.Generic;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationDraft
    {
        public FactionType FactionA { get; set; } = FactionType.Republic;
        public FactionType FactionB { get; set; } = FactionType.Separatist;
        public List<SimulationFleetEntry> FleetA { get; } = new();
        public List<SimulationFleetEntry> FleetB { get; } = new();
        public float Distance { get; set; } = 2000;
        public float TimeLimit { get; set; } = 300;
        public bool FastSpeed { get; set; }
        public bool UseHangars { get; set; } = true;
        public bool UseAbilities { get; set; } = true;
        public bool CanStart => FleetA.Count > 0 && FleetB.Count > 0;
        public SimulationSetup Build() =>
            new SimulationSetup(FactionA, FactionB, FleetA, FleetB, Distance, TimeLimit, FastSpeed, UseHangars, UseAbilities);
        public void Load(SimulationSetup setup)
        {
            FactionA = setup.FactionA; FactionB = setup.FactionB;
            FleetA.Clear(); FleetA.AddRange(setup.FleetA); FleetB.Clear(); FleetB.AddRange(setup.FleetB);
            Distance = setup.Distance; TimeLimit = setup.TimeLimit; FastSpeed = setup.FastSpeed;
            UseHangars = setup.UseHangars;
            UseAbilities = setup.UseAbilities;
        }
    }
}
