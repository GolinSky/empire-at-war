using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.ViewComponents.Weapon;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Online battles, host and client: once per second collects FPS, shots, Mirror traffic per message type,
    /// projectile pool admission and the role's numbers; appends a row to
    /// persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_net.csv and publishes overlay lines.
    /// Runs whether or not the overlay is visible.
    /// </summary>
    public sealed class NetworkStatsCollector : IInitializable, ITickable, ILateDisposable
    {
        private const float WINDOW_SECONDS = 1f;
        private const string FOLDER = "BattleTelemetry";
        private const int TOP_MESSAGE_TYPES = 6;
        private const int POOL_LINES = 6;
        private const int GRID_COLUMNS = 3;

        private static readonly (Type Type, string Messages, string Bytes)[] TRACKED_MESSAGES =
        {
            (typeof(EntityKeyframesMessage), "msg_keyframes_s", "msg_keyframes_Bps"),
            (typeof(EntityHealthMessage), "msg_health_s", "msg_health_Bps"),
            (typeof(WeaponTargetMessage), "msg_weapon_target_s", "msg_weapon_target_Bps"),
            (typeof(EntitySpawnMessage), "msg_spawn_s", "msg_spawn_Bps"),
            (typeof(ShipRouteMessage), "msg_route_s", "msg_route_Bps"),
            (typeof(ReplicatedStateMessage), "msg_state_s", "msg_state_Bps"),
            (typeof(UnitOrderStateMessage), "msg_order_state_s", "msg_order_state_Bps"),
            (typeof(AttackTargetMessage), "msg_attack_target_s", "msg_attack_target_Bps"),
        };

        private static readonly Comparison<MessageTraffic> BY_BYTES = (left, right) =>
            (right.InBytes + right.OutBytes).CompareTo(left.InBytes + left.OutBytes);
        private static readonly Comparison<ShotEffectCache> BY_WAITING = (left, right) =>
            right.WaitingCount.CompareTo(left.WaitingCount);

        private readonly INetworkRoleStats _role;
        private readonly NetworkStatusModel _model;
        private readonly ShotPoolPrewarmer _pools;
        private readonly OnlineMatchService _online;
        private readonly NetworkStatsRow _row = new NetworkStatsRow();
        private readonly List<string> _details = new List<string>();
        private readonly List<MessageTraffic> _sortedTypes = new List<MessageTraffic>();
        private readonly List<ShotEffectCache> _waitingPools = new List<ShotEffectCache>();
        private NetworkTrafficCounter _traffic;
        private NetworkStatsCsvWriter _writer;
        private string _title;
        private float _startTime;
        private float _elapsed;
        private float _maxFrame;
        private int _frames;
        private int _lastShots;
        private int _lastPoolCreated;
        private int _lastPoolBlocked;

        public NetworkStatsCollector(INetworkRoleStats role, NetworkStatusModel model, ShotPoolPrewarmer pools,
            OnlineMatchService online)
        {
            _role = role;
            _model = model;
            _pools = pools;
            _online = online;
        }

        public string CsvPath { get; private set; }

        public void Initialize()
        {
            _traffic = new NetworkTrafficCounter();
            _startTime = Time.unscaledTime;
            _lastShots = AttackSequenceDiagnostics.EmittedShots;
            string transport = DescribeTransport();
            bool host = _online.Role == OnlineRole.Host;
            _title = $"{_online.Role} · {transport} · build {Application.version}";
            string scene = SceneManager.GetActiveScene().name;
            CsvPath = Path.Combine(Application.persistentDataPath, FOLDER,
                $"{scene}_{DateTime.Now:yyyyMMdd_HHmmss}_net.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
            var comments = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("role", _online.Role.ToString()),
                new KeyValuePair<string, string>("transport", transport),
                new KeyValuePair<string, string>("build", Application.version),
                new KeyValuePair<string, string>("scene", scene),
                new KeyValuePair<string, string>("started", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("mirror_send_rate_hz",
                    (host ? NetworkServer.sendRate : NetworkClient.sendRate).ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("network_tick_rate_hz", NetworkTickSettings.NETWORK_TICK_RATE.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("render_delay_target_ms",
                    (NetworkTickSettings.TICK_INTERVAL * NetworkTickSettings.INTERPOLATION_BUFFER_TICKS * 1000f).ToString("0", CultureInfo.InvariantCulture)),
            };
            _writer = new NetworkStatsCsvWriter(new StreamWriter(CsvPath), comments);
        }

        public void Tick()
        {
            float delta = Time.unscaledDeltaTime;
            _frames++;
            _elapsed += delta;
            if (delta > _maxFrame) _maxFrame = delta;
            if (_elapsed < WINDOW_SECONDS) return;
            Collect();
            _frames = 0;
            _elapsed = 0f;
            _maxFrame = 0f;
        }

        public void LateDispose()
        {
            _traffic.Dispose();
            _writer.Dispose();
        }

        private void Collect()
        {
            float seconds = _elapsed;
            int shots = AttackSequenceDiagnostics.EmittedShots;
            var window = new NetworkStatsWindow(seconds, _frames / seconds, (shots - _lastShots) / seconds);
            _lastShots = shots;

            _row.Begin();
            _details.Clear();
            _row.Set("begun", _online.HasBegun ? 1 : 0);
            _row.Set("fps", window.Fps);
            _row.Set("frame_max_ms", _maxFrame * 1000f);
            _row.Set("shots_s", window.ShotsPerSecond);
            CollectTraffic(seconds);
            CollectPools(seconds);
            _role.Collect(_row, _details, window);
            _row.End();
            _writer.Write(Time.unscaledTime - _startTime, _row);
            _traffic.ResetWindow();
            _model.Publish(BuildLines());
        }

        private void CollectTraffic(float seconds)
        {
            _row.Set("in_Bps", _traffic.InBytes / seconds);
            _row.Set("in_msgs_s", _traffic.InMessages / seconds);
            _row.Set("out_Bps", _traffic.OutBytes / seconds);
            _row.Set("out_msgs_s", _traffic.OutMessages / seconds);
            // Each tracked type flows one way (host → client), so in + out names its traffic on either role.
            foreach ((Type type, string messages, string bytes) in TRACKED_MESSAGES)
            {
                MessageTraffic traffic = _traffic.Get(type);
                _row.Set(messages, (traffic.InMessages + traffic.OutMessages) / seconds);
                _row.Set(bytes, (traffic.InBytes + traffic.OutBytes) / seconds);
            }

            _sortedTypes.Clear();
            foreach (MessageTraffic traffic in _traffic.Types)
                if (traffic.InMessages + traffic.OutMessages > 0) _sortedTypes.Add(traffic);
            _sortedTypes.Sort(BY_BYTES);
            int count = Math.Min(TOP_MESSAGE_TYPES, _sortedTypes.Count);
            for (int i = 0; i < count; i++)
            {
                MessageTraffic traffic = _sortedTypes[i];
                _details.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0}: in {1:0}/s {2:0.0} KiB/s, out {3:0}/s {4:0.0} KiB/s", traffic.Name,
                    traffic.InMessages / seconds, traffic.InBytes / seconds / 1024f,
                    traffic.OutMessages / seconds, traffic.OutBytes / seconds / 1024f));
            }
        }

        private void CollectPools(float seconds)
        {
            int waiting = 0, inUse = 0, total = 0, created = 0, blocked = 0;
            float oldestWait = 0f, maxWait = 0f;
            _waitingPools.Clear();
            foreach (ShotEffectCache cache in _pools.Caches)
            {
                waiting += cache.WaitingCount;
                inUse += cache.InUseCount;
                total += cache.TotalCount;
                created += cache.RuntimeCreated;
                blocked += cache.AdmissionWaits;
                oldestWait = Mathf.Max(oldestWait, cache.OldestPendingWaitSeconds);
                maxWait = Mathf.Max(maxWait, cache.MaxAdmissionWaitSeconds);
                if (cache.WaitingCount > 0) _waitingPools.Add(cache);
            }

            // A battle restart clears the caches; never report negative growth.
            _row.Set("pool_waiting_shots", waiting);
            _row.Set("pool_oldest_wait_ms", oldestWait * 1000f);
            _row.Set("pool_in_use", inUse);
            _row.Set("pool_total", total);
            _row.Set("pool_created_s", Math.Max(0, created - _lastPoolCreated) / seconds);
            _row.Set("pool_blocked_attempts_s", Math.Max(0, blocked - _lastPoolBlocked) / seconds);
            _row.Set("pool_max_wait_ms", maxWait * 1000f);
            _lastPoolCreated = created;
            _lastPoolBlocked = blocked;

            if (_waitingPools.Count == 0)
            {
                _details.Add("Pools: no salvo waiting for a projectile.");
                return;
            }
            _waitingPools.Sort(BY_WAITING);
            int count = Math.Min(POOL_LINES, _waitingPools.Count);
            for (int i = 0; i < count; i++)
            {
                ShotEffectCache cache = _waitingPools[i];
                _details.Add(string.Format(CultureInfo.InvariantCulture,
                    "Pool {0} ({1}): waiting {2}, in use {3}/{4}, created at runtime {5}, oldest wait {6:0.00} s, max wait {7:0.00} s",
                    cache.PrefabName, cache.IsStrikecraftGroup ? "fighters" : "ships", cache.WaitingCount,
                    cache.InUseCount, cache.TotalCount, cache.RuntimeCreated, cache.OldestPendingWaitSeconds,
                    cache.MaxAdmissionWaitSeconds));
            }
        }

        private IReadOnlyList<string> BuildLines()
        {
            var lines = new List<string>(_row.Count / GRID_COLUMNS + _details.Count + 2) { _title };
            for (int start = 0; start < _row.Count; start += GRID_COLUMNS)
            {
                int end = Math.Min(start + GRID_COLUMNS, _row.Count);
                string line = string.Empty;
                for (int i = start; i < end; i++)
                {
                    double value = _row.GetValue(i);
                    string text = double.IsNaN(value) ? "–" : value.ToString("0.##", CultureInfo.InvariantCulture);
                    line += (i > start ? "   " : string.Empty) + _row.GetName(i) + " " + text;
                }
                lines.Add(line);
            }
            lines.Add(string.Empty);
            lines.AddRange(_details);
            return lines;
        }

        private static string DescribeTransport()
        {
            Transport active = Transport.active;
            if (!(active is MultiplexTransport multiplex)) return active.GetType().Name;
            var names = new List<string>();
            foreach (Transport transport in multiplex.transports) names.Add(transport.GetType().Name);
            return "Multiplex(" + string.Join("+", names) + ")";
        }
    }
}
