using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceApplyService
    {
        public const string RESTORE_PATH = "Library/BalanceEditor/previous-apply.json";
        public static bool CanRestore => File.Exists(RESTORE_PATH);

        public static BalanceRegistration Apply(BalanceDraft draft, Func<BalanceRegistration> rebuild)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply is available only outside Play Mode.");
            BalanceRegistration registry = rebuild();
            List<string> errors = BalanceValidation.Preflight(registry, draft);
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            if (draft.Changes.Count == 0) return registry;
            BalanceField[] fields = draft.Changes.Select(change => registry.Fields[change.Key]).Where(field => !field.InheritsSource
                || !draft.Changes.Any(change => change.Key == field.SourceKey && change.After == field.DraftValue(draft))).ToArray();
            HashSet<string> paths = fields.Select(field => field.AssetPath).ToHashSet();
            BalanceUnit[] derived = registry.Units.Where(unit => unit.Prefab != null && (paths.Contains(AssetDatabase.GetAssetPath(unit.Prefab))
                    || fields.Any(field => field.SharedMountSource && field.Users.Contains(unit))))
                .GroupBy(unit => unit.Data).Select(group => group.First()).Where(unit => unit.Data is EmpireAtWar.Entities.Ship.Data.ShipData
                    || unit.Data is EmpireAtWar.Entities.Squadrons.Data.SquadronData).ToArray();
            foreach (BalanceUnit unit in derived) paths.Add(AssetDatabase.GetAssetPath(unit.Data));
            if (paths.Any(path => EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path))))
                throw new InvalidOperationException("A target has unsaved external edits. Save that asset, refresh the inventory, and resolve its diff first.");
            BalanceApplyRecord record = new BalanceApplyRecord
            {
                Files = paths.OrderBy(path => path).Select(path => new BalanceFileSnapshot { Path = path, Before = Convert.ToBase64String(File.ReadAllBytes(path)) }).ToList()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(RESTORE_PATH));
            File.WriteAllText(RESTORE_PATH, JsonUtility.ToJson(record, true));
            List<string> written = new List<string>();
            try
            {
                foreach (IGrouping<string, BalanceField> asset in fields.GroupBy(field => field.AssetPath))
                {
                    written.Add(asset.Key);
                    if (asset.Key.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) WritePrefab(asset.Key, asset, draft);
                    else foreach (IGrouping<UnityEngine.Object, BalanceField> target in asset.GroupBy(field => field.Target)) Write(target.Key, target, draft);
                }
                AssetDatabase.SaveAssets();
                foreach (string path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                foreach (BalanceUnit unit in derived)
                {
                    string path = AssetDatabase.GetAssetPath(unit.Data);
                    if (!written.Contains(path)) written.Add(path);
                    BalanceDerivedData.Rebuild(unit);
                }
                AssetDatabase.SaveAssets();
                BalanceRegistration saved = rebuild();
                if (saved.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", saved.Errors));
                foreach (BalanceChange change in draft.Changes)
                    if (!saved.Fields.TryGetValue(change.Key, out BalanceField field) || field.Read() != change.After)
                        throw new InvalidOperationException("Read-back differs: " + change.Key);
                foreach (BalanceFileSnapshot file in record.Files) file.AfterHash = Hash(file.Path);
                record.Complete = true;
                File.WriteAllText(RESTORE_PATH, JsonUtility.ToJson(record, true));
                draft.Changes.Clear();
                draft.UndoStates.Clear();
                draft.RedoStates.Clear();
                return saved;
            }
            catch (Exception exception)
            {
                string rollback;
                try { RestoreFiles(record); rollback = "Rollback imported and verified."; File.Delete(RESTORE_PATH); }
                catch (Exception failure) { rollback = "Rollback failed: " + failure.Message + ". Recovery snapshot: " + RESTORE_PATH; }
                throw new InvalidOperationException($"Apply failed. Written/attempted assets: {string.Join(", ", written)}. {rollback}\n{exception.Message}", exception);
            }
        }

        public static void RestorePrevious()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Restore is available only outside Play Mode.");
            BalanceApplyRecord record = JsonUtility.FromJson<BalanceApplyRecord>(File.ReadAllText(RESTORE_PATH));
            if (!record.Complete) throw new InvalidOperationException("Previous apply failed. Inspect its recovery snapshot before restoring.");
            foreach (BalanceFileSnapshot file in record.Files)
                if (Hash(file.Path) != file.AfterHash || EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(file.Path)))
                    throw new InvalidOperationException("Restore conflicts with a later edit: " + file.Path);
            RestoreFiles(record);
            File.Delete(RESTORE_PATH);
        }

        private static void WritePrefab(string path, IEnumerable<BalanceField> fields, BalanceDraft draft)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (IGrouping<UnityEngine.Object, BalanceField> target in fields.GroupBy(field => field.Target))
                    Write(BalancePrefabBindings.InContents(target.Key, root), target, draft);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) throw new IOException("Could not save prefab: " + path);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Write(UnityEngine.Object target, IEnumerable<BalanceField> fields, BalanceDraft draft)
        {
            using (SerializedObject serialized = new SerializedObject(target))
            {
                foreach (BalanceField field in fields)
                    BalanceValue.Write(field.Property(serialized), field.Kind, draft.Changes.Single(change => change.Key == field.Key).After);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (target is Component && PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                EditorUtility.SetDirty(target);
            }
        }

        private static void RestoreFiles(BalanceApplyRecord record)
        {
            foreach (BalanceFileSnapshot file in record.Files) File.WriteAllBytes(file.Path, Convert.FromBase64String(file.Before));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (BalanceFileSnapshot file in record.Files) AssetDatabase.ImportAsset(file.Path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            // The restore is an exact disk snapshot; compare before reserialization, which can legitimately normalize YAML.
            foreach (BalanceFileSnapshot file in record.Files)
                if (!File.ReadAllBytes(file.Path).SequenceEqual(Convert.FromBase64String(file.Before))) throw new IOException("Rollback read-back differs: " + file.Path);
            AssetDatabase.ForceReserializeAssets(record.Files.Select(file => file.Path).ToList());
            AssetDatabase.SaveAssets();
        }

        private static string Hash(string path)
        {
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
