using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloInvocationPolicy
    {
        public static bool AllowsAssetWrites(string jobDirectory)
        {
            string activePath = Path.Combine(AloImportLayout.StateRoot, "Invocation/ActivePolicy.json");
            if (File.Exists(activePath) && (bool?)JObject.Parse(File.ReadAllText(activePath))["allowAssetMutation"] != true)
                return false;
            JObject input = JObject.Parse(File.ReadAllText(Path.Combine(jobDirectory, "Input.json")));
            return input["policy"] is JObject policy && (bool?)policy["allowAssetMutation"] == true;
        }

        public static void RequireAssetWrite(string jobDirectory, IEnumerable<string> paths)
        {
            if (!AllowsAssetWrites(jobDirectory))
                throw new InvalidOperationException("ASSET_WRITE_FORBIDDEN: this invocation permits tooling and read-only checks only.");
            foreach (string path in paths)
                AloRecipeSemanticValidator.SafePath(path, true);
        }
    }
}
