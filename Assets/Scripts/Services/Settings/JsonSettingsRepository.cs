using System;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>
    /// Stores settings in one JSON file. Writes go to a temporary file first and replace the saved file atomically,
    /// keeping one backup. An unreadable file is copied aside for diagnosis and its backup is loaded instead.
    /// </summary>
    public sealed class JsonSettingsRepository : ISettingsRepository
    {
        private const string FILE_NAME = "settings.json";
        private const string BACKUP_SUFFIX = ".bak";
        private const string TEMP_SUFFIX = ".tmp";
        private const string CORRUPT_SUFFIX = ".corrupt";

        private readonly string _path;

        /// <param name="directory">Folder that holds the settings file, e.g. <c>Application.persistentDataPath</c>.</param>
        public JsonSettingsRepository(string directory)
        {
            _path = Path.Combine(directory, FILE_NAME);
        }

        public SettingsLoadStatus Load(out SettingsData data)
        {
            string backupPath = _path + BACKUP_SUFFIX;
            if (!File.Exists(_path) && !File.Exists(backupPath))
            {
                data = null;
                return SettingsLoadStatus.Missing;
            }

            if (TryRead(_path, out data))
            {
                return SettingsLoadStatus.Loaded;
            }

            if (File.Exists(_path))
            {
                PreserveUnreadableFile();
            }

            return TryRead(backupPath, out data) ? SettingsLoadStatus.Loaded : SettingsLoadStatus.Unreadable;
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

        private void PreserveUnreadableFile()
        {
            try
            {
                File.Copy(_path, _path + CORRUPT_SUFFIX, true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not copy unreadable settings file '{_path}': {exception.Message}");
            }
        }

        // Corrupt JSON and a file locked by another process (antivirus, cloud sync) both count as unreadable.
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
            catch (Exception exception) when (exception is ArgumentException || exception is IOException ||
                                              exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings file '{path}' is unreadable: {exception.Message}");
                return false;
            }
        }
    }
}
