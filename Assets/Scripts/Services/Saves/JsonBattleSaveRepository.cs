using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Entities.Game;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Stores each save as <c>Saves/&lt;Mode&gt;/&lt;slot&gt;.json</c>. Writes go to a temporary file first and then
    /// replace the saved file, so a failed write keeps the previous save. Battle Simulation has no folder.
    /// </summary>
    public sealed class JsonBattleSaveRepository : IBattleSaveRepository
    {
        private const string SAVES_FOLDER = "Saves";
        private const string EXTENSION = ".json";
        private const string TEMP_SUFFIX = ".tmp";

        private readonly string _root;

        /// <param name="directory">Folder that holds the <c>Saves</c> folder, e.g. <c>Application.persistentDataPath</c>.</param>
        public JsonBattleSaveRepository(string directory)
        {
            _root = Path.Combine(directory, SAVES_FOLDER);
        }

        public void Save(BattleSave save)
        {
            string path = GetPath(save.Header.Mode, save.Header.Slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tempPath = path + TEMP_SUFFIX;
            File.WriteAllText(tempPath, save.ToJson());
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        public BattleSave Load(GameMode mode, BattleSaveSlot slot)
        {
            string path = GetPath(mode, slot);
            // The mode folder may not exist yet; an empty slot reports the same way either way.
            if (!File.Exists(path)) throw new FileNotFoundException($"No {mode} save in slot '{slot.Name}'.", path);
            BattleSave save = Read(path);
            if (save.Header.Mode != mode)
                throw new InvalidDataException($"Save '{path}' is a {save.Header.Mode} save, not {mode}.");
            if (!save.Header.IsCompatible)
                throw new InvalidDataException(
                    $"Save '{path}' is version {save.Header.Version}; this build loads version {BattleSaveHeader.CURRENT_VERSION}.");
            return save;
        }

        public void Delete(GameMode mode, BattleSaveSlot slot) => File.Delete(GetPath(mode, slot));

        public IReadOnlyList<BattleSaveHeader> List(GameMode mode)
        {
            string folder = GetFolder(mode);
            if (!Directory.Exists(folder)) return Array.Empty<BattleSaveHeader>();
            List<BattleSaveHeader> headers = new List<BattleSaveHeader>();
            foreach (string path in Directory.GetFiles(folder, "*" + EXTENSION))
            {
                try
                {
                    headers.Add(Read(path).Header);
                }
                catch (Exception exception) when (exception is InvalidDataException || exception is IOException ||
                                                  exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Skipping unreadable save '{path}': {exception.Message}");
                }
            }

            return headers.OrderByDescending(header => header.SavedAtUtc).ToArray();
        }

        private string GetPath(GameMode mode, BattleSaveSlot slot) => Path.Combine(GetFolder(mode), slot.Name + EXTENSION);

        private string GetFolder(GameMode mode)
        {
            if (mode != GameMode.Skirmish && mode != GameMode.Survival)
                throw new ArgumentException($"{mode} battles are not saved.", nameof(mode));
            return Path.Combine(_root, mode.ToString());
        }

        private static BattleSave Read(string path)
        {
            BattleSave save;
            try
            {
                save = BattleSave.FromJson(File.ReadAllText(path));
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException($"Save '{path}' is not valid JSON: {exception.Message}", exception);
            }

            if (save == null || save.Header == null || save.Setup == null)
                throw new InvalidDataException($"Save '{path}' has no header or battle setup.");
            return save;
        }
    }
}
