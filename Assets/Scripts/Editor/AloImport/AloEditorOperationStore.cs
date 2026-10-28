using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public sealed class AloEditorOperationStore
    {
        private readonly string _directory;
        private readonly string _session;

        public AloEditorOperationStore(string directory, string session)
        {
            _directory = directory;
            _session = session;
        }

        public JObject Read()
        {
            string path = Path.Combine(_directory, "EditorOperation.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject { ["state"] = "idle" };
        }

        public JObject Execute(string jobId, string operationId, string inputHash, Func<JObject> action)
        {
            Directory.CreateDirectory(_directory);
            using var gate = new FileStream(Path.Combine(_directory, "EditorOperation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            JObject current = Read();
            string history = Path.Combine(_directory, "Operations", Guid.Parse(operationId).ToString("D") + ".json");
            if (File.Exists(history))
            {
                JObject previous = JObject.Parse(File.ReadAllText(history));
                RequireIdentity(previous, jobId, operationId, inputHash);
                return previous;
            }
            if ((string)current["state"] != "idle" && (string)current["state"] != "succeeded")
                throw new InvalidOperationException("OPERATION_UNRESOLVED: reconcile " + current["operationId"] + " before admitting another Editor operation.");
            Guid.Parse(jobId);
            using var process = Process.GetCurrentProcess();
            var record = new JObject
            {
                ["jobId"] = jobId, ["operationId"] = operationId, ["inputHash"] = inputHash,
                ["sessionNonce"] = _session, ["editorPid"] = process.Id, ["editorProcessStartUtc"] = process.StartTime.ToUniversalTime().ToString("O"),
                ["state"] = "running", ["startedUtc"] = DateTime.UtcNow.ToString("O")
            };
            Save(record);
            try
            {
                JObject result = action();
                if (result.DescendantsAndSelf().OfType<JProperty>().Any(property => property.Name == "success" && property.Value.Type == JTokenType.Boolean && !(bool)property.Value))
                    throw new InvalidOperationException("Editor action returned a nested failure: " + result);
                record["result"] = result;
                record["state"] = "succeeded";
                record["completedUtc"] = DateTime.UtcNow.ToString("O");
                Directory.CreateDirectory(Path.GetDirectoryName(history));
                Write(history, record);
                Save(record);
                return record;
            }
            catch (Exception error)
            {
                record["state"] = "needs-recovery";
                record["error"] = error.ToString();
                Save(record);
                throw;
            }
        }

        public JObject Recover(string jobId, string operationId, string inputHash, Func<JObject> reconcile)
        {
            Directory.CreateDirectory(_directory);
            using var gate = new FileStream(Path.Combine(_directory, "EditorOperation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            JObject record = Read();
            string history = Path.Combine(_directory, "Operations", Guid.Parse(operationId).ToString("D") + ".json");
            if (File.Exists(history))
            {
                JObject terminal = JObject.Parse(File.ReadAllText(history));
                RequireIdentity(terminal, jobId, operationId, inputHash);
                if ((string)record["operationId"] == operationId)
                {
                    JObject cleared = (JObject)terminal.DeepClone();
                    cleared["state"] = "idle";
                    Save(cleared);
                }
                return terminal;
            }
            RequireIdentity(record, jobId, operationId, inputHash);
            if ((string)record["state"] == "running" && (string)record["sessionNonce"] == _session)
                throw new InvalidOperationException("OPERATION_UNRESOLVED: operation belongs to the current live Editor session.");
            if ((string)record["state"] != "running" && (string)record["state"] != "needs-recovery")
                throw new InvalidOperationException("RECOVERY_CONFLICT: operation is not interrupted.");
            record["recovery"] = reconcile();
            record["state"] = "failed";
            record["code"] = "NO_ORIGINAL_WRITES";
            record["reconciledUtc"] = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(Path.GetDirectoryName(history));
            Write(history, record);
            JObject idle = (JObject)record.DeepClone();
            idle["state"] = "idle";
            Save(idle);
            return record;
        }

        private static void RequireIdentity(JObject record, string jobId, string operationId, string hash)
        {
            if ((string)record["jobId"] != jobId || (string)record["operationId"] != operationId || (string)record["inputHash"] != hash)
                throw new InvalidOperationException("RECOVERY_CONFLICT: operation identity or frozen input differs.");
        }

        private void Save(JObject record) => Write(Path.Combine(_directory, "EditorOperation.json"), record);

        private static void Write(string path, JObject record)
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
