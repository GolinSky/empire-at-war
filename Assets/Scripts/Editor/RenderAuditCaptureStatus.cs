using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    internal static class RenderAuditCaptureStatus
    {
        private const string ROOT_DIRECTORY = "Logs/RenderAudit";
        private const string LAST_STATUS_PATH_KEY = "EmpireAtWar.RenderAudit.LastStatusPath";

        internal static string CreateCaptureDirectory(string captureName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            var directory = Path.Combine(ROOT_DIRECTORY, $"{timestamp}-{captureName}");
            Directory.CreateDirectory(directory);
            return directory;
        }

        internal static Dictionary<string, object> CreateResult(string status, string message)
        {
            return new Dictionary<string, object>
            {
                ["status"] = status,
                ["message"] = message,
                ["captured_at_utc"] = DateTime.UtcNow.ToString("O"),
                ["unity_frame"] = UnityEngine.Time.frameCount
            };
        }

        internal static void WriteJson(string directory, string fileName, object value)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
            if (fileName == "capture-status.json")
            {
                SessionState.SetString(LAST_STATUS_PATH_KEY, path);
            }
        }

        internal static object ReadLastStatus()
        {
            var path = SessionState.GetString(LAST_STATUS_PATH_KEY, string.Empty);
            return File.Exists(path) ? JToken.Parse(File.ReadAllText(path)) : null;
        }

        internal static string GetStatusPath(string directory)
        {
            return Path.Combine(directory, "capture-status.json");
        }

        internal static string SanitizeFileName(string value)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "unnamed" : value;
        }
    }
}
