using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloUnityArtBuilder
    {
        public static List<(string Source, string Destination, JToken Definition)> Preflight(JObject recipe, JObject manifest, string stagingDirectory)
        {
            AloRecipeSchemaValidator.Validate(recipe);
            string staging = Path.GetFullPath(stagingDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var files = new List<(string Source, string Destination, JToken Definition)>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!(manifest["files"] is JArray entries) || entries.Count == 0) throw new InvalidOperationException("Missing staged art manifest files.");
            foreach (JToken file in entries)
            {
                if (!(file["materialRemaps"] is JArray) || file["baselineSha256"] == null)
                    throw new InvalidOperationException("Staged art requires explicit materialRemaps and baselineSha256.");
                JToken output = recipe["integration"]["outputs"].Single(item => (string)item["role"] == (string)file["outputRole"]);
                string destination = (string)output["path"];
                AloRecipeSemanticValidator.SafePath(destination, true);
                string extension = Path.GetExtension(destination).ToLowerInvariant();
                if (extension != ".fbx" && extension != ".png" && extension != ".tga")
                    throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: art publication accepts FBX/PNG/TGA only.");
                string source = Path.GetFullPath(Path.Combine(staging, (string)file["source"]));
                if (!source.StartsWith(staging, StringComparison.OrdinalIgnoreCase) || !paths.Add(destination))
                    throw new InvalidOperationException("OUTPUT_CONFLICT: staging escape or duplicate destination.");
                if (Hash(source) != (string)file["sha256"]) throw new InvalidOperationException("INPUT_CHANGED: staged art hash differs.");
                string expected = output["guid"].Type == JTokenType.Null ? "" : (string)output["guid"];
                if (AssetDatabase.AssetPathToGUID(destination) != expected)
                    throw new InvalidOperationException("INPUT_CHANGED: output GUID baseline differs: " + destination);
                string baseline = file["baselineSha256"].Type == JTokenType.Null ? null : (string)file["baselineSha256"];
                if ((File.Exists(destination) ? Hash(destination) : null) != baseline)
                    throw new InvalidOperationException("INPUT_CHANGED: output content baseline differs: " + destination);
                foreach (JToken remap in file["materialRemaps"])
                {
                    if (!recipe["materials"].Any(material => (string)material["targetGuid"] == (string)remap["materialGuid"]))
                        throw new InvalidOperationException("Material remap is not a reviewed recipe target.");
                    if (AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath((string)remap["materialGuid"])) == null)
                        throw new InvalidOperationException("Missing external material " + remap["materialGuid"]);
                }
                string[] materialNames = file["materialRemaps"].Select(remap => (string)remap["sourceName"]).ToArray();
                if (materialNames.Any(string.IsNullOrEmpty) || materialNames.Distinct(StringComparer.Ordinal).Count() != materialNames.Length)
                    throw new InvalidOperationException("Duplicate or missing transport material name.");
                files.Add((source, destination, file));
            }
            return files;
        }

        public static JObject PublishFiles(JObject recipe, JObject manifest, string stagingDirectory, string jobId, string jobDirectory)
        {
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, recipe["integration"]["outputs"].Select(output => (string)output["path"]));
            string staging = Path.GetFullPath(stagingDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var files = Preflight(recipe, manifest, stagingDirectory);
            AloPublicationJournal.Prepare(jobDirectory, files.Select(file => file.Destination));
            JObject candidates = AloModelCandidateImporter.Verify(recipe, manifest, staging, jobId, jobDirectory);
            foreach (var file in files)
            {
                if (Hash(file.Source) != (string)file.Definition["sha256"]) throw new InvalidOperationException("INPUT_CHANGED: staged art changed during candidate validation.");
                string baseline = file.Definition["baselineSha256"].Type == JTokenType.Null ? null : (string)file.Definition["baselineSha256"];
                if ((File.Exists(file.Destination) ? Hash(file.Destination) : null) != baseline) throw new InvalidOperationException("INPUT_CHANGED: output changed during candidate validation.");
                Directory.CreateDirectory(Path.GetDirectoryName(file.Destination));
                if (!File.Exists(file.Destination) || Hash(file.Source) != Hash(file.Destination)) File.Copy(file.Source, file.Destination, true);
                AloPublicationJournal.Checkpoint(jobDirectory, "artCopied:" + file.Definition["outputRole"], new[] { file.Destination });
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in files.Where(item => Path.GetExtension(item.Destination).Equals(".fbx", StringComparison.OrdinalIgnoreCase)))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(file.Destination);
                AloModelImportSettings.Copy(importer, importer, (JArray)file.Definition["materialRemaps"]);
                importer.SaveAndReimport();
            }
            string[] changedPaths = files.Select(file => file.Destination).ToArray();
            AssetDatabase.ForceReserializeAssets(changedPaths);
            AssetDatabase.SaveAssets();
            AloPublicationJournal.Checkpoint(jobDirectory, "artSaved", changedPaths);
            return new JObject { ["candidateIdentity"] = candidates, ["published"] = new JArray(changedPaths) };
        }

        private static string Hash(string path)
        {
            using var algorithm = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
