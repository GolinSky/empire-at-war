using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloNoWriteRecovery
    {
        public static JObject Reconcile(string jobDirectory, string jobId)
        {
            string path = Path.Combine(jobDirectory, "PublicationJournal.json");
            var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
            JObject journal = File.Exists(path) ? JObject.Parse(File.ReadAllText(path), settings) : new JObject { ["preimages"] = new JArray() };
            if (!(journal["preimages"] is JArray preimages)) throw new InvalidOperationException("RECOVERY_CONFLICT: publication journal has no preimage inventory.");
            var conflicts = new JArray();
            foreach (JToken entry in preimages)
            {
                string asset = (string)entry["path"];
                AloRecipeSemanticValidator.SafePath(asset, false);
                if (!asset.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("RECOVERY_CONFLICT: preimage escaped Assets.");
                Compare(asset, entry["asset"], conflicts);
                Compare(asset + ".meta", entry["meta"], conflicts);
            }
            if (conflicts.Count != 0)
                throw new InvalidOperationException("RECOVERY_CONFLICT: original assets differ; no rollback performed. " + new JObject { ["conflicts"] = conflicts, ["checkpoints"] = journal["checkpoints"] });
            string owned = AloImportLayout.CandidatePath(jobId);
            if ((Directory.Exists(owned) || File.Exists(owned + ".meta")) && !AssetDatabase.DeleteAsset(owned))
                throw new InvalidOperationException("RECOVERY_CONFLICT: cannot clean owned candidate directory " + owned);
            AssetDatabase.SaveAssets();
            if (Directory.Exists(owned) || File.Exists(owned + ".meta")) throw new InvalidOperationException("RECOVERY_CONFLICT: owned candidate cleanup did not persist.");
            return new JObject { ["originalAssets"] = "unchanged", ["preimagesChecked"] = preimages.Count, ["candidateCleanup"] = "saved", ["outcome"] = "failed-before-original-writes" };
        }

        private static void Compare(string path, JToken preimage, JArray conflicts)
        {
            if (!(preimage is JObject) || preimage["exists"] == null) throw new InvalidOperationException("RECOVERY_CONFLICT: missing preimage record for " + path);
            string before = (bool)preimage["exists"] ? (string)preimage["sha256"] : null;
            string current = File.Exists(path) ? Hash(path) : null;
            if ((bool)preimage["exists"] && string.IsNullOrEmpty(before)) throw new InvalidOperationException("RECOVERY_CONFLICT: missing preimage hash " + path);
            if (current != before) conflicts.Add(new JObject { ["path"] = path, ["preimageSha256"] = before, ["currentSha256"] = current });
        }

        private static string Hash(string path)
        {
            using var algorithm = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
