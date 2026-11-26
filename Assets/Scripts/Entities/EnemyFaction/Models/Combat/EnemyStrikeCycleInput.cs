namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>What <see cref="EnemyStrikeCycleModel"/> reads each decision: the strike group, its target and its safe point as plain numbers.</summary>
    public struct EnemyStrikeCycleInput
    {
        public float Now { get; set; }

        /// <summary>The group fights near its station, in a local fight, or meets the target on a hunt or assault.</summary>
        public bool IsInFight { get; set; }

        /// <summary>A known hostile heavy cruiser, capital, battlecruiser or dreadnought is the strike target.</summary>
        public bool HasTarget { get; set; }

        public int StrikerCount { get; set; }
        public int NonStrikerCount { get; set; }

        /// <summary>Strength of the whole group against the target and its support; 1 is an even fight.</summary>
        public float GroupAdvantage { get; set; }

        public float StrikerMaxShields { get; set; }
        /// <summary>Shield points per second the strikers regenerate together.</summary>
        public float StrikerShieldRegen { get; set; }
        /// <summary>Shield damage per second the target and its support deal to the strikers.</summary>
        public float HostileShieldDps { get; set; }
        public float StrikerShieldDps { get; set; }
        public float StrikerHullDps { get; set; }
        public float StrikerPiercingDps { get; set; }

        public float TargetShields { get; set; }
        public float TargetMaxShields { get; set; }
        public float TargetShieldRegen { get; set; }
        public float TargetMaxHull { get; set; }
        /// <summary>Hull points per second hostile repairers in range restore on the target.</summary>
        public float HostileRepairPerSecond { get; set; }
        /// <summary>The owner of the target has Field Repairs: it repairs itself when nothing keeps it under fire.</summary>
        public bool TargetSelfRepairs { get; set; }

        public bool HasSafePoint { get; set; }
        /// <summary>One way from the fight to the safe point at the speed of the slowest striker.</summary>
        public float TravelSeconds { get; set; }
        public float SlowestStrikerSpeed { get; set; }
        public float TargetSpeed { get; set; }
        /// <summary>The safe point is behind the own station.</summary>
        public bool SafePointIsStationCover { get; set; }
        public float SafePointDistance { get; set; }
        public float TargetWeaponRange { get; set; }

        /// <summary>The target stands under hostile station or platform guns the group cannot beat.</summary>
        public bool TargetUnderUnbeatableDefenses { get; set; }

        /// <summary>Seconds the target and its support need to destroy the group ships that are no strikers.</summary>
        public float LeftBehindSurvivalSeconds { get; set; }

        /// <summary>Strikers whose shields regenerate; the fractions below average only these.</summary>
        public int RegeneratingStrikerCount { get; set; }
        public float AverageShieldFraction { get; set; }
        public float AverageHullFraction { get; set; }

        public bool HasStationPressure { get; set; }
        /// <summary>Own Field Repairs research or a working repairer covers the safe point.</summary>
        public bool IsHullRepairAvailable { get; set; }
        /// <summary>The target followed the strikers: its weapons reach the safe point.</summary>
        public bool IsChased { get; set; }
        /// <summary>No striker is still flying to the safe point.</summary>
        public bool HaveStrikersArrived { get; set; }
    }
}
