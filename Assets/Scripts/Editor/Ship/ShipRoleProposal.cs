using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// Writes a role proposal (<see cref="ShipRoleRules.Propose"/>) into every ship data asset and the faction marks
    /// (<see cref="ShipRoleRules.ProposeFactionMarks"/>) into every faction definition, for review by hand. Overwrites the
    /// current roles, so run it only for a fresh proposal.
    /// </summary>
    [EditorToolInfo("Propose AI ship roles within each faction and class, and faction marks (overwrites current roles).")]
    public static class ShipRoleProposal
    {
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        [MenuItem("Tools/Empire At War/Units/Ships/Propose Ship Roles")]
        public static string ProposeAll()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            List<ShipRoleEntry> roster = ShipRoleRoster.Load();
            var report = new StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FactionDefinition)))
            {
                FactionDefinition faction =
                    AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                List<ShipRoleEntry> fleet = roster.Where(entry => entry.Faction == faction.FactionType).ToList();
                UnitRole marks = ShipRoleRules.ProposeFactionMarks(fleet, roster);
                Write(faction, "shipMarks", marks);
                report.AppendLine($"{faction.FactionType} marks: {marks}");
            }
            foreach (ShipRoleEntry entry in roster.Where(entry => entry.IsOwner))
            {
                UnitRole roles = ShipRoleRules.Propose(entry, ShipRoleRoster.ClassMates(roster, entry), weapons);
                Write(entry.Ship, "roles", roles);
                report.AppendLine($"{entry.Faction} {entry.Ship.name} ({entry.Ship.ShipClass}): {roles}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShipRoleProposal] {roster.Count(entry => entry.IsOwner)} ships.\n{report}");
            return report.ToString();
        }

        private static void Write(Object asset, string property, UnitRole value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(property).intValue = (int)value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
    }
}
