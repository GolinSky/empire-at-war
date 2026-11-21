using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Data;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// Writes a first role proposal (<see cref="ShipRoleRules.Propose"/>) into every ship data asset, for review by
    /// hand. Overwrites the current roles, so run it only for a fresh proposal.
    /// </summary>
    [EditorToolInfo("Propose AI ship roles from class medians and weapon loadouts (overwrites current roles).")]
    public static class ShipRoleProposal
    {
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        [MenuItem("Tools/Empire At War/Units/Ships/Propose Ship Roles")]
        public static string ProposeAll()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            List<ShipData> ships = AssetDatabase.FindAssets("t:ShipData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
            var report = new StringBuilder();
            foreach (ShipData ship in ships)
            {
                List<ShipData> classMates = ships.Where(mate => mate.ShipClass == ship.ShipClass).ToList();
                ShipRole roles = ShipRoleRules.Propose(ship, classMates, weapons);
                var serialized = new SerializedObject(ship);
                serialized.FindProperty("roles").intValue = (int)roles;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ship);
                report.AppendLine($"{ship.name} ({ship.ShipClass}): {roles}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShipRoleProposal] {ships.Count} ships.\n{report}");
            return report.ToString();
        }
    }
}
