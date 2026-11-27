using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Editor and development builds: one CSV row per caster each time its auto-power outcome or shield gates change,
    /// in <c>BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_autopower.csv</c>.
    /// </summary>
    public static class AutoPowerTelemetry
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float FLUSH_INTERVAL = 10f;
        private const string HEADER = "timeScaledSinceStartup,timeUnscaledSinceStartup,player,entityId,shipClass,reason," +
            "role,ability,hullPct,shieldPct,shieldLoss,canRegenShields,regenBlock,assessed,power,urgent,hostileShip," +
            "hostileNearby,engaged,shieldSlot,shieldSlotTimeLeft,activeAbilities";
        private static readonly Dictionary<long, long> _lastKey = new Dictionary<long, long>();
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
            _lastKey.Clear();
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, "BattleTelemetry",
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}_autopower.csv");
        }
#endif

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Record(IEntity entity, IShipAbilityFacade caster, IShipAutoPowerFacade automation,
            AutoPowerReason reason, AutoPowerRole role, ShipAbilityId ability, AutoPowerShieldLoss shieldLoss,
            AutoPowerSituation? situation)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return;
            float shieldPct = caster.Health.ShieldPercentage;
            bool canRegen = automation != null && automation.CanRegenerateShields;
            bool hostileShip = situation.HasValue && situation.Value.HostileShip;
            // Repeated identical outcomes are skipped; a shield gate crossing (40 %, 60 %) still writes a row.
            int band = shieldPct < 0.4f ? 0 : shieldPct < 0.6f ? 1 : 2;
            long key = (long)reason | (long)role << 8 | (long)ability << 16 | (long)band << 32 | (long)shieldLoss << 34 |
                (canRegen ? 1L << 36 : 0L) | (hostileShip ? 1L << 37 : 0L) | (situation.HasValue ? 1L << 38 : 0L);
            if (_lastKey.TryGetValue(entity.Id, out long last) && last == key) return;
            _lastKey[entity.Id] = key;

            ShipAbilitySlot shieldSlot = null;
            StringBuilder active = new StringBuilder();
            foreach (ShipAbilitySlot slot in caster.Slots)
            {
                if (slot.Definition.AutoPower == AutoPowerRole.Shields) shieldSlot = slot;
                if (slot.State != ShipAbilityState.Active) continue;
                if (active.Length > 0) active.Append(';');
                active.Append(slot.Id);
            }
            AutoPowerSituation s = situation.GetValueOrDefault();
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F2},{1:F2},{2},{3},{4},{5},{6},{7},{8:F2},{9:F2},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20:F1},{21}",
                Time.time, Time.unscaledTime, entity.Owner, entity.Id, caster.Health.ShipClass, reason, role, ability,
                caster.Health.HullPercentage, shieldPct, shieldLoss, canRegen ? 1 : 0, RegenBlock(caster, automation),
                situation.HasValue ? 1 : 0, situation.HasValue ? s.Power.ToString() : "",
                situation.HasValue ? (s.Urgent ? "1" : "0") : "", situation.HasValue ? (hostileShip ? "1" : "0") : "",
                situation.HasValue ? (s.HostileNearby ? "1" : "0") : "", situation.HasValue ? (s.Engaged ? "1" : "0") : "",
                shieldSlot == null ? "none" : shieldSlot.State.ToString(), shieldSlot?.TimeLeft ?? 0f, active));
            if (Time.unscaledTime >= _nextFlush) Flush();
#endif
        }

        /// <summary>Forgets a removed caster so a reused id starts a fresh row history.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Forget(long entityId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lastKey.Remove(entityId);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Why shield regeneration is zero, which keeps a shield boost from being chosen; empty while it regenerates.
        private static string RegenBlock(IShipAbilityFacade caster, IShipAutoPowerFacade automation)
        {
            if (automation == null) return "noAutoPower";
            if (automation.CanRegenerateShields) return "";
            if (automation.MaxShields <= 0f) return "noShields";
            if (caster.Health.IsLostShieldGenerator) return "generatorLost";
            if (automation.ShieldSuppressed) return "suppressed";
            return "recovering";
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
