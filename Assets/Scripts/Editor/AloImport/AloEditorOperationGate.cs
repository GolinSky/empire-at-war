using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloEditorOperationGate
    {
        private static readonly AloEditorOperationStore _store = new AloEditorOperationStore(
            AloImportLayout.CoordinatorRoot, Guid.NewGuid().ToString("N"));

        public static JObject Read() => _store.Read();

        public static JObject Execute(string jobId, string operationId, string inputHash, Func<JObject> action)
        {
            RequireReady();
            return _store.Execute(jobId, operationId, inputHash, action);
        }

        public static JObject Recover(string jobId, string operationId, string inputHash, Func<JObject> reconcile)
        {
            RequireReady();
            return _store.Recover(jobId, operationId, inputHash, reconcile);
        }

        private static void RequireReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("EDITOR_BUSY: ALO operations require idle Edit Mode.");
            // PipelineTestRunner.GetTestStatus uses these persisted records across domain reloads.
            if (File.Exists("Temp/pipeline_test_request.json"))
                throw new InvalidOperationException("EDITOR_BUSY: Unity Pipeline has a pending or active test request.");
            if (File.Exists("Temp/pipeline_test_status.json") && (string)JObject.Parse(File.ReadAllText("Temp/pipeline_test_status.json"))["status"] == "running")
                throw new InvalidOperationException("EDITOR_BUSY: Unity Pipeline tests are running.");
        }
    }
}
