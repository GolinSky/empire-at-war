using System;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>
    /// Stores settings in one JSON file under the persistent-data directory.
    /// Writes go to a temporary file first and replace the saved file atomically, keeping one backup.
    /// </summary>
    public sealed class JsonSettingsRepository : ISettingsRepository
    {
        private const string FILE_NAME = "settings.json";
        private const string BACKUP_SUFFIX = ".bak";
        private const string TEMP_SUFFIX = ".tmp";
        private const string CORRUPT_SUFFIX = ".corrupt";

        private readonly string _path = Path.Combine(Application.persistentDataPath, FILE_NAME);

        public bool TryLoad(out SettingsData data)
        {
            if (TryRead(_path, out data))
            {
                return true;
            }

            string backupPath = _path + BACKUP_SUFFIX;
            if (File.Exists(_path))
            {
                // Keep the unreadable file for diagnosis, then fall back to the last good copy.
                File.Copy(_path, _path + CORRUPT_SUFFIX, true);
                Debug.LogWarning($"Settings file '{_path}' is unreadable; loading its backup.");
            }

            return TryRead(backupPath, out data);
        }

        public void Save(SettingsData data)
        {
            string tempPath = _path + TEMP_SUFFIX;
            File.WriteAllText(tempPath, data.ToJson());
            if (File.Exists(_path))
            {
                File.Replace(tempPath, _path, _path + BACKUP_SUFFIX);
            }
            else
            {
                File.Move(tempPath, _path);
            }
        }

        private static bool TryRead(string path, out SettingsData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                data = SettingsData.FromJson(File.ReadAllText(path));
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Settings file '{path}' has invalid JSON: {exception.Message}");
                return false;
            }
        }
    }
}
