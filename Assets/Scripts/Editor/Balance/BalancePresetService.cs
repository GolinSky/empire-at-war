using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalancePresetService
    {
        public const string PRESET_FOLDER = "Assets/Settings/Data/Balance/Presets";

        public static void Save(BalancePreset preset, BalanceRegistration registry, BalanceDraft draft, string scope)
        {
            if (registry.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", registry.Errors));
            var fields = scope == "Full registered set" ? registry.Fields.Values.ToList()
                : draft.Changes.Select(change => registry.Fields.TryGetValue(change.Key, out BalanceField field) ? field
                    : throw new InvalidOperationException("Missing draft target: " + change.Key)).ToList();
            if (fields.Count == 0) throw new InvalidOperationException("The selected preset scope is empty.");
            var values = new System.Collections.Generic.List<BalanceSnapshot>();
            foreach (BalanceField field in fields)
            {
                string value = field.DraftValue(draft);
                string error = field.Validate(value);
                if (error.Length != 0) throw new InvalidOperationException(field.Context + "/" + field.Label + ": " + error);
                values.Add(new BalanceSnapshot(field.Key, field.PresetSchema, value));
            }
            preset.SetValues(scope == "Full registered set", values.OrderBy(entry => entry.Key).ToList());
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
        }

        public static void Load(BalancePreset preset, BalanceRegistration registry, BalanceDraft draft)
        {
            if (preset == null) throw new InvalidOperationException("Choose a preset first.");
            if (preset.Version != BalancePreset.CURRENT_VERSION) throw new InvalidOperationException("Unsupported preset version: " + preset.Version);
            foreach (BalanceSnapshot entry in preset.Values)
            {
                // Version 1 snapshots also included live inheritance/consumer state after these five schema parts.
                if (!registry.Fields.TryGetValue(entry.Key, out BalanceField field) || field.PresetSchema != string.Join("|", entry.Schema.Split('|').Take(5)))
                    throw new InvalidOperationException("Preset target is missing or incompatible: " + entry.Key);
                string error = field.Validate(entry.Value);
                if (error.Length != 0) throw new InvalidOperationException(field.Label + ": " + error);
            }
            if (preset.FullScope && !preset.Values.Select(entry => entry.Key).ToHashSet().SetEquals(registry.Fields.Keys))
                throw new InvalidOperationException("Full preset scope differs from the current inventory. Resolve newly added or removed targets first.");
            draft.ReplaceScope(preset.Values.Select(entry => new BalanceSnapshot(entry.Key, registry.Fields[entry.Key].Schema, entry.Value)).ToList(),
                registry.Fields.Values.ToDictionary(field => field.Key, field => field.Snapshot()));
        }

        public static void EnsureFolder()
        {
            string current = "Assets";
            foreach (string folder in PRESET_FOLDER.Split('/').Skip(1))
            {
                string next = current + "/" + folder;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, folder);
                current = next;
            }
        }
    }
}
