using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Dev-only timeline of what each AI decided and bought, to explain battle results the damage telemetry cannot.
    /// Writes persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_ai.csv every few seconds and starts a new file on
    /// every scene load. Decisions are written when they change and sampled every few seconds in between.
    /// </summary>
    public static class EnemyAiTelemetry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float FLUSH_INTERVAL = 10f;
        private const float SAMPLE_INTERVAL = 5f;
        private const string FOLDER = "BattleTelemetry";
        private const string HEADER =
            "time,kind,player,faction,state,committed,ownShips,body,rallying,enemyShips,fleetAdvantage," +
            "localAdvantage,localThreat,baseThreat,category,counter,unit,price,money,reason";

        private static readonly StringBuilder _lines = new StringBuilder();
        private static readonly Dictionary<PlayerId, float> _nextSample = new Dictionary<PlayerId, float>();
        private static readonly Dictionary<(PlayerId, Entities.CaptureSites.SiteFacilityType), int> _satelliteCounts = new();
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
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Flush();
            Reset(scene.name);
        }

        private static void Reset(string sceneName)
        {
            _lines.Clear();
            _nextSample.Clear();
            _satelliteCounts.Clear();
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, FOLDER,
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}_ai.csv");
        }
#endif

        public static void RecordDecision(PlayerId player, FactionType faction, bool changed,
            EnemyStrategicDecision decision, EnemyStrategicSnapshot snapshot, int bodyCount, int rallyingCount)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Battles only: EditMode tests drive the AI without the play-mode file setup.
            if (!Application.isPlaying) return;
            float now = Time.time;
            bool sampleDue = !_nextSample.TryGetValue(player, out float next) || now >= next;
            if (!changed && !sampleDue) return;
            _nextSample[player] = now + SAMPLE_INTERVAL;
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F1},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10:F2},{11:F2},{12},{13:F2},,,,,,\"{14}\"",
                now, changed ? "decision" : "sample", player, faction, decision.State, decision.CommittedShipCount,
                snapshot.OwnShipCount, bodyCount, rallyingCount, snapshot.EnemyShipCount, snapshot.FleetAdvantage,
                snapshot.LocalAdvantage, snapshot.HasLocalThreat ? 1 : 0, snapshot.BaseThreatRatio, decision.Reason));
            FlushIfDue();
#endif
        }

        public static void RecordPurchase(PlayerId player, FactionType faction, EnemyStrategicState state,
            EnemyProductionCategory category, bool counter, string unit, int price, float money)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F1},purchase,{1},{2},{3},,,,,,,,,,{4},{5},{6},{7},{8:F0},",
                Time.time, player, faction, state, category, counter ? 1 : 0, unit, price, money));
            FlushIfDue();
#endif
        }

        public static void RecordSatellite(PlayerId player, FactionType faction, EnemyDefenseChoice choice,
            float money, IReadOnlyList<EnemyDefenseChoice> topChoices)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            var key = (player, choice.Type);
            _satelliteCounts.TryGetValue(key, out int count);
            _satelliteCounts[key] = ++count;
            StringBuilder reason = new StringBuilder(choice.Reason);
            reason.AppendFormat(CultureInfo.InvariantCulture, "; pad={0}; built={1}; top=", choice.SiteIndex, count);
            foreach (EnemyDefenseChoice candidate in topChoices)
                reason.AppendFormat(CultureInfo.InvariantCulture, " {0}/{1}:{2:F3}", candidate.SiteIndex, candidate.Type, candidate.Score);
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F1},satellite,{1},{2},,,,,,,,,,,Defense,,{3},{4:F0},{5:F0},\"{6}\"",
                Time.time, player, faction, choice.Type, choice.Cost, money, reason));
            FlushIfDue();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void FlushIfDue()
        {
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
