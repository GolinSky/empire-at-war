using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Timeline of what each AI decided and bought, to explain battle results the damage telemetry cannot.
    /// Writes persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_ai.csv every few seconds and starts a new file on
    /// every scene load. Editor and development builds write every row: decisions when they change, sampled every
    /// few seconds in between. Release builds write the compact log: purchases, deployments, economy, ownership and
    /// station rows, armada responses, one state sample, one allocation row and one production-wait row per AI every
    /// <see cref="COMPACT_INTERVAL"/> seconds,
    /// and keep only the newest <see cref="COMPACT_FILE_LIMIT"/> files. The file stays on the machine that runs the AI.
    /// </summary>
    public static class EnemyAiTelemetry
    {
        private const float FLUSH_INTERVAL = 10f;
        private const float SAMPLE_INTERVAL = 5f;
        private const float COMPACT_INTERVAL = 15f;
        private const int COMPACT_FILE_LIMIT = 20;
        private const string FILE_SUFFIX = "_ai.csv";
        private const string FOLDER = "BattleTelemetry";
        private const string HEADER =
            "time,kind,player,faction,state,committed,ownShips,body,rallying,enemyShips,fleetAdvantage," +
            "localAdvantage,localThreat,baseThreat,category,counter,unit,price,money,reason";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly bool _compact = false;
#else
        private static readonly bool _compact = true;
#endif
        private static readonly HashSet<string> _compactKinds = new HashSet<string>
        {
            "deployment", "deploymentBlocked", "deploymentForced", "deploymentEmergency", "economy", "station",
            "siteBuild", "placement", "placementDeferred", "recoveryMode", "surplusShip", "spawnBeacon", "patrolWing",
            "wingZoneLeft", "raidSkip", "captureBlocked", "zoneOwner", "zoneAlloc", "production-wait", "armadaResponse",
            // W57: counter-pick reasons, one order count per group per minute, and D3 drop requests.
            "counter-selection", "groupOrders", "reinforcementDrop"
        };

        /// <summary>Compact kinds written at most once per AI every <see cref="COMPACT_INTERVAL"/> seconds.</summary>
        private static readonly HashSet<string> _throttledKinds = new HashSet<string> { "zoneAlloc", "production-wait" };

        private static readonly StringBuilder _lines = new StringBuilder();
        private static readonly Dictionary<(PlayerId, string), float> _nextAllocation = new Dictionary<(PlayerId, string), float>();
        private static readonly Dictionary<PlayerId, float> _nextSample = new Dictionary<PlayerId, float>();
        private static readonly Dictionary<PlayerId, string> _lastObjective = new Dictionary<PlayerId, string>();
        private static readonly Dictionary<(PlayerId, Entities.CaptureSites.SiteFacilityType), int> _satelliteCounts = new();
        private static string _path;
        private static float _nextFlush;
        private static bool _writeFailed;

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
            _nextSample.Clear();
            _nextAllocation.Clear();
            _lastObjective.Clear();
            _satelliteCounts.Clear();
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            _path = Path.Combine(Application.persistentDataPath, FOLDER,
                $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}{FILE_SUFFIX}");
        }

        /// <summary>Release builds keep the newest <see cref="COMPACT_FILE_LIMIT"/> logs so the folder cannot grow without end.</summary>
        private static void PruneOldFiles()
        {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(_path), "*" + FILE_SUFFIX);
            if (files.Length <= COMPACT_FILE_LIMIT) return;
            Array.Sort(files, (left, right) => File.GetLastWriteTimeUtc(left).CompareTo(File.GetLastWriteTimeUtc(right)));
            for (int i = 0; i < files.Length - COMPACT_FILE_LIMIT; i++) File.Delete(files[i]);
        }

        public static void RecordDecision(PlayerId player, FactionType faction, bool changed,
            EnemyStrategicDecision decision, EnemyStrategicSnapshot snapshot, int bodyCount, int rallyingCount,
            EnemyStrategicContext context = null, string actionReason = "", float recoverySeconds = 0f)
        {
            // Battles only: EditMode tests drive the AI without the play-mode file setup.
            if (!Application.isPlaying) return;
            float now = Time.time;
            if (context != null && !_compact)
            {
                EnemyCaptureObjective current = context.CaptureObjective;
                string objectiveKey = (current == null ? "none" : $"{current.Kind}/{current.Id}/{current.Owner}") +
                    $"/defense:{(context.DefenseTarget == null ? 0 : context.DefenseTarget.Id)}/anchor:{context.RetreatAnchor}";
                changed |= !_lastObjective.TryGetValue(player, out string previous) || previous != objectiveKey;
                _lastObjective[player] = objectiveKey;
            }
            bool sampleDue = !_nextSample.TryGetValue(player, out float next) || now >= next;
            if (_compact ? !sampleDue : !changed && !sampleDue) return;
            _nextSample[player] = now + (_compact ? COMPACT_INTERVAL : SAMPLE_INTERVAL);
            string details = decision.Reason + string.Format(CultureInfo.InvariantCulture,
                "; localRaw={0:F3}; localSmoothed={1:F3}; fleetSmoothed={2:F3}; stateHeldSeconds={3:F1}; recoveryMode={4}",
                snapshot.LocalRaw, snapshot.LocalAdvantage, snapshot.FleetAdvantage,
                context == null ? 0f : context.StateHeldSeconds, snapshot.RecoveryMode);
            if (context != null)
            {
                // Round 9 (W43): assault gate parts, critical cause, idle group ships, local rating origin.
                int idleShips = 0;
                foreach (var ship in context.Ships)
                    if (ship.CurrentOrder == UnitOrderType.None) idleShips++;
                details += string.Format(CultureInfo.InvariantCulture,
                    "; {0}; critical={1}; idleShips={2}; localOrigin={3}", context.AssaultGate,
                    context.StationPressure == null ? "none" : context.StationPressure.CriticalCause, idleShips,
                    context.LocalOrigin);
                // Round 10: reach-based fleet rating (W63), trade correction (W67), late-game gate (W48), retreat latch (W49).
                details += string.Format(CultureInfo.InvariantCulture,
                    "; fleetRaw={0:F3}; tradeCorrection={1:F2}; lateGame={2}; retreatPointReached={3}",
                    snapshot.FleetRaw, snapshot.TradeCorrection, snapshot.IsLateGame, snapshot.HasReachedRetreatPoint);
            }
            if (context != null && !_compact)
            {
                EnemyCaptureObjective objective = context.CaptureObjective;
                var target = decision.State == EnemyStrategicState.DefendBase ? context.DefenseTarget :
                    decision.State == EnemyStrategicState.EngageLocal ? context.LocalThreatTarget :
                    decision.State == EnemyStrategicState.AssaultBase ? context.AssaultTarget ?? context.EnemyBaseTarget :
                    decision.State == EnemyStrategicState.RaidStructures ? context.RaidTarget :
                    decision.State == EnemyStrategicState.HuntFleet ? context.EnemyFleetTarget : null;
                Vector3 destination = context.OrderDestination.HasValue
                    ? context.OrderDestination.Value : decision.State == EnemyStrategicState.RetreatValue && context.RetreatAnchor.HasValue
                    ? context.RetreatAnchor.Value : decision.State == EnemyStrategicState.CaptureZone
                        ? context.CaptureTarget : target == null ? Vector3.zero :
                            target.GetFacade<IEntityTransformFacade>().Transform.position;
                details += string.Format(CultureInfo.InvariantCulture,
                    "; clock=Unity.Time.time scaled seconds since play (not save battleTime); objective={0}; type={1}; owner={2}; " +
                    "destination=({3:F1} {4:F1} {5:F1}); progress={6:F3}; regrouped={7}; recoverySeconds={8:F1}; " +
                    "stationPressure={9}; defenseTarget={10}; target={11}; action={12}; selection={13}; pressureSource={14}; " +
                    "orderDestination=({3:F1} {4:F1} {5:F1}); defensePointReason={15}; heldShips={16}; " +
                    "visibleAdvantage={17:F2}; unseenInvestment={18:F0}; economyPressure={19:F2}; teamHeldZones={20}",
                    objective == null ? "none" : objective.Id, objective == null ? "none" : objective.Kind.ToString(),
                    objective == null ? "none" : objective.Owner.ToString(), destination.x, destination.y, destination.z,
                    objective == null ? 0f : objective.Progress, snapshot.IsRegrouped, recoverySeconds,
                    snapshot.HasStationPressure, context.DefenseTarget == null ? 0 : context.DefenseTarget.Id,
                    target == null ? 0 : target.Id, actionReason, context.RecoveryReason, context.StationPressureReason,
                    context.DefensePointReason, context.HeldShips, context.VisibleAdvantage,
                    context.UnseenInvestment, context.EconomyPressure, context.TeamHeldZones);
                StringBuilder assignments = new StringBuilder(details);
                foreach (var ship in context.Ships)
                {
                    Vector3 offset = ship.WorldPosition - destination;
                    offset.y = 0f;
                    assignments.AppendFormat(CultureInfo.InvariantCulture,
                        "; ship={0}/{1}/distance:{2:F1}", ship.EntityId, ship.CurrentOrder, offset.magnitude);
                }
                details = assignments.ToString();
            }
            details = details.Replace("\"", "\"\"");
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F1},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10:F2},{11:F2},{12},{13:F2},,,,,,\"{14}\"",
                now, changed ? "decision" : "sample", player, faction, decision.State, decision.CommittedShipCount,
                snapshot.OwnShipCount, bodyCount, rallyingCount, snapshot.EnemyShipCount, snapshot.FleetAdvantage,
                snapshot.LocalAdvantage, snapshot.HasLocalThreat ? 1 : 0, snapshot.BaseThreatRatio, details));
            FlushIfDue();
        }

        public static void RecordEvent(PlayerId player, FactionType faction, string kind, string reason)
        {
            if (!Application.isPlaying) return;
            if (_compact)
            {
                if (!_compactKinds.Contains(kind)) return;
                if (_throttledKinds.Contains(kind))
                {
                    if (_nextAllocation.TryGetValue((player, kind), out float next) && Time.time < next) return;
                    _nextAllocation[(player, kind)] = Time.time + COMPACT_INTERVAL;
                }
            }
            string[] columns = new string[20];
            columns[0] = Time.time.ToString("F1", CultureInfo.InvariantCulture);
            columns[1] = kind;
            columns[2] = player.ToString();
            columns[3] = faction.ToString();
            columns[19] = "\"" + reason.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
            _lines.AppendLine(string.Join(",", columns));
            FlushIfDue();
        }

        public static void RecordPurchase(PlayerId player, FactionType faction, EnemyStrategicState state,
            EnemyProductionCategory category, bool counter, string unit, int price, float money,
            bool critical = false, float etaSeconds = 0f, float timeToLoss = float.PositiveInfinity,
            string savingUnit = "", float savingFor = 0f)
        {
            if (!Application.isPlaying) return;
            _lines.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F1},purchase,{1},{2},{3},,,,,,,,,,{4},{5},{6},{7},{8:F0},critical={9};etaSeconds={10:F1};timeToLoss={11:F1};saving={12};savingFor={13:F1}",
                Time.time, player, faction, state, category, counter ? 1 : 0, unit, price, money,
                critical, etaSeconds, timeToLoss, savingUnit, savingFor));
            FlushIfDue();
        }

        public static void RecordZoneOwner(PlayerId player, string id, PlayerId previousOwner, PlayerId owner,
            string kind)
        {
            RecordEvent(player, default, "zoneOwner",
                FormattableString.Invariant(
                    $"id={id};kind={kind};from={previousOwner};owner={owner};battleTime={BattleClock.CurrentElapsed:F1}"));
        }

        public static void RecordSatellite(PlayerId player, FactionType faction, EnemyDefenseChoice choice,
            float money, IReadOnlyList<EnemyDefenseChoice> topChoices)
        {
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
        }

        private static void FlushIfDue()
        {
            if (Time.unscaledTime >= _nextFlush) Flush();
        }

        private static void Flush()
        {
            _nextFlush = Time.unscaledTime + FLUSH_INTERVAL;
            if (_lines.Length == 0) return;
            if (_writeFailed)
            {
                _lines.Clear();
                return;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                if (!File.Exists(_path))
                {
                    File.WriteAllText(_path, HEADER + System.Environment.NewLine);
                    if (_compact) PruneOldFiles();
                }
                File.AppendAllText(_path, _lines.ToString());
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // A full or locked disk must not break the AI tick that reports here: the log stops for this run.
                _writeFailed = true;
                Debug.LogWarning($"AI battle log stopped: {exception.Message}");
            }
            _lines.Clear();
        }
    }
}
