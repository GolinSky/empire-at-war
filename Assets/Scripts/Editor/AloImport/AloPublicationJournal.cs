using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloPublicationJournal
    {
        public static void Prepare(string jobDirectory, IEnumerable<string> paths, JObject ownedKeys = null)
        {
            string path = Path.Combine(jobDirectory, "PublicationJournal.json");
            JObject journal = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject
            {
                ["schemaVersion"] = 1, ["state"] = "prepared", ["preimages"] = new JArray(), ["checkpoints"] = new JArray()
            };
            var preimages = (JArray)journal["preimages"];
            string backupDirectory = Path.Combine(jobDirectory, "Preimages");
            Directory.CreateDirectory(backupDirectory);
            foreach (string assetPath in paths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                AloRecipeSemanticValidator.SafePath(assetPath, false);
                if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Publication preimages must belong to Assets.");
                if (preimages.Any(entry => string.Equals((string)entry["path"], assetPath, StringComparison.OrdinalIgnoreCase))) continue;
                int index = preimages.Count;
                var entry = new JObject { ["path"] = assetPath, ["asset"] = Capture(assetPath, backupDirectory, index + "-asset.bin"), ["meta"] = Capture(assetPath + ".meta", backupDirectory, index + "-meta.bin") };
                preimages.Add(entry);
                Write(path, journal);
            }
            if (ownedKeys != null)
            {
                if (!(journal["ownedKeys"] is JObject)) journal["ownedKeys"] = new JObject();
                foreach (JProperty key in ownedKeys.Properties())
                {
                    if (journal["ownedKeys"][key.Name] != null && !JToken.DeepEquals(journal["ownedKeys"][key.Name], key.Value))
                        throw new InvalidOperationException("RECOVERY_CONFLICT: journal ownership key changed " + key.Name);
                    journal["ownedKeys"][key.Name] = key.Value.DeepClone();
                }
            }
            Write(path, journal);
        }

        public static void Checkpoint(string jobDirectory, string checkpoint, IEnumerable<string> paths)
        {
            string path = Path.Combine(jobDirectory, "PublicationJournal.json");
            JObject journal = JObject.Parse(File.ReadAllText(path));
            var postimages = new JArray();
            foreach (string assetPath in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!journal["preimages"].Any(entry => string.Equals((string)entry["path"], assetPath, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Missing publication preimage for " + assetPath);
                postimages.Add(new JObject { ["path"] = assetPath, ["assetSha256"] = HashOrNull(assetPath), ["metaSha256"] = HashOrNull(assetPath + ".meta") });
            }
            ((JArray)journal["checkpoints"]).Add(new JObject { ["name"] = checkpoint, ["utc"] = DateTime.UtcNow.ToString("O"), ["postimages"] = postimages });
            journal["state"] = checkpoint;
            Write(path, journal);
        }

        private static JObject Capture(string source, string backupDirectory, string backupName)
        {
            if (!File.Exists(source)) return new JObject { ["exists"] = false };
            byte[] bytes = File.ReadAllBytes(source);
            string backup = Path.Combine(backupDirectory, backupName);
            if (File.Exists(backup)) throw new InvalidOperationException("RECOVERY_CONFLICT: unrecorded preimage backup exists " + backupName);
            using (var stream = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            return new JObject { ["exists"] = true, ["sha256"] = Hash(bytes), ["backup"] = "Preimages/" + backupName };
        }

        private static JToken HashOrNull(string path) => File.Exists(path) ? new JValue(Hash(File.ReadAllBytes(path))) : JValue.CreateNull();

        private static string Hash(byte[] bytes)
        {
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void Write(string path, JObject journal)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(journal.ToString());
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
