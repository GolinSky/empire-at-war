using System;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Stores hint progress in its own JSON file, separate from settings. Writes replace the file atomically.</summary>
    public sealed class JsonHintProgressRepository : IHintProgressRepository
    {
        private const string FILE_NAME = "hint_progress.json";
        private const string TEMP_SUFFIX = ".tmp";

        private readonly string _path;
        private HintProgress _progress;

        /// <param name="directory">Folder that holds the file, e.g. <c>Application.persistentDataPath</c>.</param>
        public JsonHintProgressRepository(string directory)
        {
            _path = Path.Combine(directory, FILE_NAME);
        }

        public HintProgress Load()
        {
            if (_progress == null) _progress = Read();
            return _progress;
        }

        public void Save(HintProgress progress)
        {
            string tempPath = _path + TEMP_SUFFIX;
            File.WriteAllText(tempPath, progress.ToJson());
            if (File.Exists(_path)) File.Replace(tempPath, _path, null);
            else File.Move(tempPath, _path);
        }

        public void Reset()
        {
            HintProgress progress = Load();
            progress.Clear();
            try
            {
                Save(progress);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not save reset hint progress: {exception.Message}");
            }
        }

        private HintProgress Read()
        {
            if (!File.Exists(_path)) return new HintProgress();
            try
            {
                return HintProgress.FromJson(File.ReadAllText(_path));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException ||
                                              exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Hint progress file '{_path}' is unreadable; hints start fresh: {exception.Message}");
                return new HintProgress();
            }
        }
    }
}
