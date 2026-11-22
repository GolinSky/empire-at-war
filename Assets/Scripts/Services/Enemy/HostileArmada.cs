using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Known hostile ships standing within one engagement radius of each other: where they are, what they weigh,
    /// where they move, and the team asset they move toward or stand at.
    /// </summary>
    public sealed class HostileArmada
    {
        public ForceComposition Strength { get; } = new ForceComposition();
        public Vector3 Centre { get; internal set; }
        /// <summary>Movement of the centre in units per second, measured between samples.</summary>
        public Vector3 Velocity { get; internal set; }
        public int Ships { get; internal set; }
        /// <summary>The team asset the armada threatens; <see cref="ArmadaAssetKind.None"/> when it threatens none.</summary>
        public ArmadaAssetKind AssetKind { get; internal set; }
        public Vector3 AssetPosition { get; internal set; }
        public bool ThreatensAsset => AssetKind != ArmadaAssetKind.None;
        /// <summary>Capital, battlecruiser and dreadnought ships in the group.</summary>
        public float CapitalShips => Strength.Units(ShipClass.Capital) + Strength.Units(ShipClass.BattleCruiser) +
            Strength.Units(ShipClass.Dreadnought);

        internal Vector3 SampleCentre { get; set; }
        internal float SampleTime { get; set; }
    }
}
