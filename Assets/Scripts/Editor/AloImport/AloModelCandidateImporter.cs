using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloModelCandidateImporter
    {
        public static JObject Verify(JObject recipe, JObject manifest, string stagingDirectory, string jobId, string jobDirectory)
        {
            string owned = AloImportLayout.CandidatePath(jobId);
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, new[] { owned });
            if (Directory.Exists(owned) || File.Exists(owned + ".meta"))
                throw new InvalidOperationException("RECOVERY_CONFLICT: candidate directory already exists; reconcile the previous operation first.");
            JToken[] files = manifest["files"].Where(file =>
            {
                string destination = (string)recipe["integration"]["outputs"].Single(output => (string)output["role"] == (string)file["outputRole"])["path"];
                return Path.GetExtension(destination).Equals(".fbx", StringComparison.OrdinalIgnoreCase);
            }).ToArray();
            if (files.Length == 0) throw new InvalidOperationException("Missing candidate FBX files.");
            var journal = new JObject { ["jobId"] = jobId, ["ownedPath"] = owned, ["state"] = "planned", ["models"] = new JArray() };
            string journalPath = Path.Combine(jobDirectory, "UnityCandidateJournal.json");
            Save(journalPath, journal);
            try
            {
                foreach (JToken file in files)
                {
                    string destination = (string)recipe["integration"]["outputs"].Single(output => (string)output["role"] == (string)file["outputRole"])["path"];
                    string candidate = owned + "/" + (string)file["outputRole"] + "/" + Path.GetFileName(destination);
                    string inventory = Path.Combine(jobDirectory, "CandidateEvidence", (string)file["outputRole"] + "-Identity.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(candidate));
                    File.Copy(Path.Combine(stagingDirectory, (string)file["source"]), candidate);
                    journal["state"] = "importing";
                    ((JArray)journal["models"]).Add(new JObject { ["outputRole"] = file["outputRole"], ["candidate"] = candidate, ["baseline"] = destination, ["identityInventory"] = inventory });
                    Save(journalPath, journal);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    var baseline = (ModelImporter)AssetImporter.GetAtPath(destination);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(candidate);
                    AloModelImportSettings.Copy(baseline, importer, (JArray)file["materialRemaps"]);
                    importer.SaveAndReimport();
                    AloModelIdentityVerifier.Verify(destination, candidate, inventory);
                }
                journal["state"] = "passed";
                Save(journalPath, journal);
                return new JObject { ["state"] = "passed", ["models"] = files.Length };
            }
            catch (Exception error)
            {
                journal["state"] = "failed";
                journal["error"] = error.ToString();
                Save(journalPath, journal);
                throw;
            }
            finally
            {
                if (Directory.Exists(owned) && !AssetDatabase.DeleteAsset(owned))
                    throw new InvalidOperationException("RECOVERY_CONFLICT: failed to remove owned candidate import " + owned);
                AssetDatabase.SaveAssets();
                journal["candidateCleanup"] = "saved";
                Save(journalPath, journal);
            }
        }

        private static void Save(string path, JObject record)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(record.ToString());
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
