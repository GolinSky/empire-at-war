using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloImportLayout
    {
        public static string StateRoot => Read("stateRoot");
        public static string CandidateRoot => Read("candidateRoot");
        public static string JobsRoot => Path.Combine(StateRoot, "Jobs");
        public static string CoordinatorRoot => Path.Combine(StateRoot, "Coordinator");

        public static string CandidatePath(string jobId) => CandidateRoot + "/" + Guid.Parse(jobId).ToString("D");

        private static string Read(string key)
        {
            JObject layout = JObject.Parse(File.ReadAllText("Tools/AloImport/layout.json"));
            if ((int)layout["schemaVersion"] != 1) throw new InvalidOperationException("Unsupported ALO layout version");
            string path = (string)layout[key];
            AloRecipeSemanticValidator.SafePath(path, false);
            if (key == "stateRoot" && !path.StartsWith("Tools/AloImport/", StringComparison.Ordinal)) throw new InvalidOperationException("ALO state must stay inside Tools/AloImport.");
            if (key == "candidateRoot" && !path.StartsWith("Assets/Sandbox/", StringComparison.Ordinal)) throw new InvalidOperationException("ALO candidate imports must stay inside Assets/Sandbox.");
            return path;
        }
    }
}
