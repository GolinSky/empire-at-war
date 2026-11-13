using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Models.Players;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// Dev-only squadron and hangar timeline for every player: hangar launches, bay and hangar shutdowns,
    /// squadron losses and periodic squadron order/position samples. Writes
    /// persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_squadrons.csv; clock = scaled <c>Time.time</c>.
    /// A launch row links squadron <c>entityId</c> to its <c>carrierId</c>.
    /// </summary>
    public static class SquadronTelemetry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float FLUSH_INTERVAL = 10f;
        private const string HEADER = "time,kind,player,entityId,unit,order,targetId,engagedId,x,z," +
            "targetDistance,hull,shields,carrierId,bay,reason";
        private static readonly StringBuilder _lines = new StringBuilder();
        private static string _path;
        private static float _nextFlush;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Reset(SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= Flush;
            Application.quitting += Flush;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR
        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Flush();
        }
#endif

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Flush();
            Reset(scene.name);
        }

        private static void Reset(string sceneName)
        {
            _lines.Clear();
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, "BattleTelemetry",
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}_squadrons.csv");
        }
#endif

        /// <summary>Squadron row: start, sample (the squadron paces its own samples) or destroyed.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Squadron(string kind, PlayerId owner, long entityId, string unit, string order,
            long targetId, long engagedId, Vector3 position, float targetDistance, float hull, float shields,
            string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            Append(string.Format(CultureInfo.InvariantCulture,
                "{0:F2},{1},{2},{3},{4},{5},{6},{7},{8:F1},{9:F1},{10:F1},{11:F1},{12:F1},,,\"{13}\"",
                Time.time, kind, owner, entityId, unit, order, targetId, engagedId, position.x, position.z,
                targetDistance, hull, shields, reason.Replace("\"", "\"\"")));
#endif
        }

        /// <summary>Hangar row: launch, bayDisabled, bayEnabled, hangarShutdown, hangarResumed, hangarReleased.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Hangar(string kind, PlayerId owner, long carrierId, int bay, long squadronId, string unit,
            Vector3 position, string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            Append(string.Format(CultureInfo.InvariantCulture,
                "{0:F2},{1},{2},{3},{4},,,,{5:F1},{6:F1},,,,{7},{8},\"{9}\"",
                Time.time, kind, owner, squadronId, unit, position.x, position.z, carrierId, bay,
                reason.Replace("\"", "\"\"")));
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void Append(string line)
        {
            _lines.AppendLine(line);
            if (Time.unscaledTime >= _nextFlush) Flush();
        }

        private static void Flush()
        {
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            if (_lines.Length == 0) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            if (!File.Exists(_path)) File.WriteAllText(_path, HEADER + System.Environment.NewLine);
            File.AppendAllText(_path, _lines.ToString());
            _lines.Clear();
        }
#endif
    }
}
