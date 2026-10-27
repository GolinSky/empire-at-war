using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// Dev-only record of what each unit type really fired, dealt and lost in a battle, for comparison with the Battle Simulator.
    /// Rewrites persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;.csv every few seconds and starts a new file on every scene load.
    /// </summary>
    public static class BattleTelemetry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float FLUSH_INTERVAL = 10f;
        private const string FOLDER = "BattleTelemetry";

        private sealed class ShooterRow
        {
            public int Shots;
            public float FirstShot = -1f;
            public float LastShot;
        }

        private sealed class ImpactRow
        {
            public int Impacts;
            public int FleetCommandImpacts;
            public double Raw;
            public double Shield;
            public double Hull;
            public int Kills;
        }

        private static readonly Dictionary<(FactionType, string), ShooterRow> _shooters =
            new Dictionary<(FactionType, string), ShooterRow>();
        private static readonly Dictionary<(FactionType, string, DamageType, FactionType, string), ImpactRow> _impacts =
            new Dictionary<(FactionType, string, DamageType, FactionType, string), ImpactRow>();
        private static string _path;
        private static float _startTime;
        private static float _nextFlush;
        private static bool _dirty;

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
            _shooters.Clear();
            _impacts.Clear();
            _dirty = false;
            _startTime = Time.unscaledTime;
            _nextFlush = _startTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, FOLDER,
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
#endif

        public static void RecordShot(FactionType faction, string unit)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_shooters.TryGetValue((faction, unit), out ShooterRow row))
                _shooters.Add((faction, unit), row = new ShooterRow());
            row.Shots++;
            float time = Time.time;
            if (row.FirstShot < 0f) row.FirstShot = time;
            row.LastShot = time;
            Touch();
#endif
        }

        public static void RecordImpact(FactionType faction, string unit, DamageType damageType, bool fleetCommand,
            FactionType targetFaction, string target, float raw, float shieldLoss, float hullLoss, bool killed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var key = (faction, unit, damageType, targetFaction, target);
            if (!_impacts.TryGetValue(key, out ImpactRow row)) _impacts.Add(key, row = new ImpactRow());
            row.Impacts++;
            if (fleetCommand) row.FleetCommandImpacts++;
            row.Raw += raw;
            row.Shield += shieldLoss;
            row.Hull += hullLoss;
            if (killed) row.Kills++;
            Touch();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void Touch()
        {
            _dirty = true;
            if (Time.unscaledTime >= _nextFlush) Flush();
        }

        private static void Flush()
        {
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            if (!_dirty) return;
            _dirty = false;
            CultureInfo invariant = CultureInfo.InvariantCulture;
            var text = new StringBuilder();
            text.AppendLine(string.Format(invariant, "# battle seconds (unscaled),{0:F0}", Time.unscaledTime - _startTime));
            text.AppendLine("section,faction,unit,damageType,targetFaction,target,impacts,fleetCommandImpacts,rawDamage,shieldLoss,hullLoss,effective/raw,kills,shots,activeSeconds");
            foreach (KeyValuePair<(FactionType, string), ShooterRow> pair in _shooters)
                text.AppendLine(string.Format(invariant, "shots,{0},{1},,,,,,,,,,,{2},{3:F0}",
                    pair.Key.Item1, pair.Key.Item2, pair.Value.Shots, pair.Value.LastShot - pair.Value.FirstShot));
            foreach (KeyValuePair<(FactionType, string, DamageType, FactionType, string), ImpactRow> pair in _impacts)
            {
                ImpactRow row = pair.Value;
                text.AppendLine(string.Format(invariant, "impact,{0},{1},{2},{3},{4},{5},{6},{7:F0},{8:F0},{9:F0},{10:F3},{11},,",
                    pair.Key.Item1, pair.Key.Item2, pair.Key.Item3, pair.Key.Item4, pair.Key.Item5, row.Impacts,
                    row.FleetCommandImpacts, row.Raw, row.Shield, row.Hull,
                    row.Raw > 0 ? (row.Shield + row.Hull) / row.Raw : 0, row.Kills));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, text.ToString());
        }
#endif
    }
}
