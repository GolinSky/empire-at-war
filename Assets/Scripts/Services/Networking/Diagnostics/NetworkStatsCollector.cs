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
    /// projectile pool admission, connection links and the role's numbers; appends rows to
    /// persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_&lt;role&gt;_{net,pools,links}.csv
    /// (<see cref="NetworkStatsFiles"/>) and publishes overlay lines. Runs whether or not the overlay is visible.
    /// </summary>
    public sealed class NetworkStatsCollector : IInitializable, ITickable, ILateDisposable
    {
        private const float WINDOW_SECONDS = 1f;
        private const string FOLDER = "BattleTelemetry";
        private const int TOP_MESSAGE_TYPES = 6;
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

        private readonly INetworkRoleStats _role;
        private readonly NetworkStatusModel _model;
        private readonly ShotPoolPrewarmer _pools;
        private readonly OnlineMatchService _online;
        private readonly NetworkStatsRow _row = new NetworkStatsRow();
        private readonly List<string> _details = new List<string>();
        private readonly List<NetworkLinkSample> _links = new List<NetworkLinkSample>();
        private readonly List<MessageTraffic> _sortedTypes = new List<MessageTraffic>();
        private readonly ShotPoolStats _poolStats = new ShotPoolStats();
        private NetworkTrafficCounter _traffic;
        private NetworkStatsFiles _files;
        private string _title;
        private float _startTime;
        private float _elapsed;
        private float _maxFrame;
        private int _frames;
        private int _lastShots;

        public NetworkStatsCollector(INetworkRoleStats role, NetworkStatusModel model, ShotPoolPrewarmer pools,
            OnlineMatchService online)
        {
            _role = role;
            _model = model;
            _pools = pools;
            _online = online;
        }

        /// <summary>The _net.csv path; null when the files could not be created.</summary>
        public string CsvPath => _files.StatsPath;

        public void Initialize()
        {
            _traffic = new NetworkTrafficCounter();
            _startTime = Time.unscaledTime;
            _lastShots = AttackSequenceDiagnostics.EmittedShots;
            string transport = DescribeTransport();
            bool host = _online.Role == OnlineRole.Host;
            _title = $"{_online.Role} · {transport} · build {Application.version}";
            string scene = SceneManager.GetActiveScene().name;
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
            _files = NetworkStatsFiles.Open(Path.Combine(Application.persistentDataPath, FOLDER),
                $"{scene}_{DateTime.Now:yyyyMMdd_HHmmss}_{_online.Role}", comments);
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
            _files.Dispose();
        }

        private void Collect()
        {
            float seconds = _elapsed;
            int shots = AttackSequenceDiagnostics.EmittedShots;
            var window = new NetworkStatsWindow(seconds, _frames / seconds, (shots - _lastShots) / seconds);
            _lastShots = shots;

            _row.Begin();
            _details.Clear();
            _links.Clear();
            _row.Set("begun", _online.HasBegun ? 1 : 0);
            _row.Set("fps", window.Fps);
            _row.Set("frame_max_ms", _maxFrame * 1000f);
            _row.Set("shots_s", window.ShotsPerSecond);
            CollectTraffic(seconds);
            _poolStats.Collect(_pools.Caches, seconds, _row, _details);
            _role.Collect(_row, _details, _links, window);
            _row.End();
            double battleSeconds = Time.unscaledTime - _startTime;
            _files.WriteStats(battleSeconds, _row);
            foreach (ShotPoolSample pool in _poolStats.WaitingPools) _files.WritePool(battleSeconds, pool);
            foreach (NetworkLinkSample link in _links)
            {
                _details.Add(SteamLinkAggregate.FormatLink(link));
                _files.WriteLink(battleSeconds, link);
            }
            _files.Flush();
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
