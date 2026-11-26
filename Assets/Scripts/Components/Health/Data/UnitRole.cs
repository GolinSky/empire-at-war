using System;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>
    /// What a ship or squadron is for, so the AI can pick by need and class instead of by price alone. Several flags combine.
    /// Durability, speed and gun flags compare a ship with the ships of its own faction and class (every faction's when
    /// that class has fewer than three ships); what a whole faction shares (Empire slow, Rebellion fast) is the
    /// faction's mark on its faction definition. The AI reads ship roles only to pick its rebuild pool
    /// (<c>EnemyRebuildRolePolicy</c>, user 2026-11-26).
    /// </summary>
    [Flags]
    public enum UnitRole
    {
        None = 0,
        /// <summary>Durability for its price near its faction's class median.</summary>
        Balanced = 1 << 0,
        /// <summary>More durable for its price than most of its faction's class.</summary>
        Brawler = 1 << 1,
        /// <summary>More guns than most of its faction's class.</summary>
        Dps = 1 << 2,
        /// <summary>Less durable for its price than most of its faction's class.</summary>
        Fragile = 1 << 3,
        /// <summary>Faster than most of its faction's class.</summary>
        Fast = 1 << 4,
        /// <summary>Slower than most of its faction's class.</summary>
        Slow = 1 << 5,
        /// <summary>Launches squadrons from two or more hangar bays.</summary>
        Carrier = 1 << 6,
        /// <summary>Missiles and torpedoes are the largest part of its guns: hull damage.</summary>
        MissileSpecialist = 1 << 7,
        /// <summary>Ion weapons are the largest part of its guns, or it has an ion ability: shield damage.</summary>
        IonSpecialist = 1 << 8,
        /// <summary>Lasers and point defense are the largest part of its guns.</summary>
        AntiStrikecraft = 1 << 9,
        /// <summary>Fights from standoff range.</summary>
        Artillery = 1 << 10,
        /// <summary>Helps other ships more than it fights (interdiction).</summary>
        Support = 1 << 11,
        /// <summary>
        /// Deals more damage to capital ships than most of its faction's class (heavy guns), or has an ability that
        /// disables them (ion pulse).
        /// </summary>
        AntiCapital = 1 << 12
    }
}
