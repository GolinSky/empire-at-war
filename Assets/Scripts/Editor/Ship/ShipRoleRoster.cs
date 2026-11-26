using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// Every ship each faction builds, from the faction definitions and the ship data map. A ship data asset two factions
    /// share appears once per faction; <see cref="ShipRoleEntry.IsOwner"/> marks the one whose folder holds it (the first
    /// faction when no folder matches), and only that entry writes its roles.
    /// </summary>
    public static class ShipRoleRoster
    {
        private const string SHIPS_DATA_PATH = "Assets/Settings/Data/Ship/Shared/ShipsData.asset";

        public static List<ShipRoleEntry> Load()
        {
            ShipsData shipsData = AssetDatabase.LoadAssetAtPath<ShipsData>(SHIPS_DATA_PATH);
            var found = new List<(ShipData Ship, FactionType Faction, float Price, string Path)>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FactionDefinition)))
            {
                FactionDefinition faction =
                    AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (KeyValuePair<ShipType, FactionData> ship in faction.Ships)
                {
                    string path = AssetDatabase.GUIDToAssetPath(shipsData.GetShipDataPath(ship.Key));
                    found.Add((AssetDatabase.LoadAssetAtPath<ShipData>(path), faction.FactionType, ship.Value.Price, path));
                }
            }
            var entries = new List<ShipRoleEntry>();
            foreach (var group in found.GroupBy(entry => entry.Ship))
            {
                var owner = group.FirstOrDefault(entry => entry.Path.Contains("/" + entry.Faction + "/"));
                if (owner.Ship == null) owner = group.First();
                foreach (var entry in group)
                    entries.Add(new ShipRoleEntry(entry.Ship, entry.Faction, entry.Price,
                        entry.Faction == owner.Faction));
            }
            return entries;
        }

        /// <summary>A faction class smaller than this compares with the class across every faction.</summary>
        public const int MIN_CLASS_MATES = 3;

        /// <summary>
        /// The ships of <paramref name="entry"/>'s faction and class, itself included. When that is fewer than
        /// <see cref="MIN_CLASS_MATES"/>, a median would only compare the ship with itself or split a pair, so it is
        /// every faction's ships of that class instead, each ship data asset once (its owner entry).
        /// </summary>
        public static List<ShipRoleEntry> ClassMates(IEnumerable<ShipRoleEntry> roster, ShipRoleEntry entry)
        {
            List<ShipRoleEntry> sameClass = roster.Where(mate => mate.Ship.ShipClass == entry.Ship.ShipClass).ToList();
            List<ShipRoleEntry> faction = sameClass.Where(mate => mate.Faction == entry.Faction).ToList();
            if (faction.Count >= MIN_CLASS_MATES) return faction;
            return sameClass.Where(mate => mate.IsOwner || mate.Ship == entry.Ship && mate.Faction == entry.Faction)
                .ToList();
        }
    }
}
