using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimUnitDefinition : IHealthData, ISquadronHealthData
    {
        public string Id;
        public string Name;
        public string Faction;
        public bool IsSquadron;
        public string Warning = "";
        public float Range;
        public float HullRepairPerSecond;
        public ShipClass ShipClass { get; set; }
        public float Hull { get; set; }
        public float Shields { get; set; }
        public float ShieldRegenerateValue { get; set; }
        public float ShieldRegenerateDelay { get; set; }
        public float MemberHull { get; set; }
        public float MemberShields { get; set; }
        public float LaserShieldDamageMultiplier { get; set; }
        public IReadOnlyList<HardPointHealth> HardPointHealth { get; set; } = Array.Empty<HardPointHealth>();
        public readonly List<SimHardPointDefinition> HardPoints = new List<SimHardPointDefinition>();
        public readonly List<SimMountDefinition> Mounts = new List<SimMountDefinition>();
    }
}
