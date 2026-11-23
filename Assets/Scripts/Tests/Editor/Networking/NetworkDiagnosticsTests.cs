using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Diagnostics;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using Steamworks;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class NetworkDiagnosticsTests
    {
        private const float TOLERANCE = 0.001f;

        [Test]
        public void KeyframeTrack_PredictsOnlyPastItsNewestKeyframe()
        {
            var track = new ClientKeyframeTrack();
            track.Add(1.0, new EntityMotion(Vector3.zero, 0f, 10f, 0f, 0f));

            Assert.That(track.TryPredict(1.0, out _), Is.False);
            Assert.That(track.TryPredict(0.5, out _), Is.False);
            Assert.That(track.TryPredict(1.5, out EntityMotion predicted), Is.True);
            // Heading 0 = +Z: half a second at 10 m/s.
            Assert.That(predicted.Position.z, Is.EqualTo(5f).Within(TOLERANCE));
        }

        [Test]
        public void ReplicaStats_CorrectionMeanAndMax_FollowRecordedErrors()
        {
            var stats = new ClientReplicaStats();
            var predicted = new EntityMotion(Vector3.zero, 10f, 0f, 0f, 0f);

            stats.RecordCorrection(new EntityMotion(new Vector3(3f, 0f, 4f), 10f, 0f, 0f, 0f), predicted);
            stats.RecordCorrection(new EntityMotion(new Vector3(1f, 0f, 0f), 350f, 0f, 0f, 0f), predicted);

            Assert.That(stats.Corrections, Is.EqualTo(2));
            Assert.That(stats.PositionErrorMax, Is.EqualTo(5f).Within(TOLERANCE));
            Assert.That(stats.PositionErrorSum / stats.Corrections, Is.EqualTo(3f).Within(TOLERANCE));
            // 350° vs 10° is a 20° wrap-around difference, not 340°.
            Assert.That(stats.HeadingErrorMax, Is.EqualTo(20f).Within(TOLERANCE));

            stats.ResetWindow();
            Assert.That(stats.Corrections, Is.Zero);
            Assert.That(stats.PositionErrorMax, Is.Zero);
        }

        [Test]
        public void ReplicaStats_CountsChunksMissingFromAPartlyReceivedTick()
        {
            var stats = new ClientReplicaStats();
            stats.RecordChunk(10, 3, 69);
            stats.RecordChunk(10, 3, 69);
            stats.RecordChunk(11, 1, 5);
            // A late chunk of an older tick counts as received traffic but never as loss.
            stats.RecordChunk(10, 3, 69);
            stats.RecordChunk(12, 2, 10);

            Assert.That(stats.LostChunks, Is.EqualTo(1));
            Assert.That(stats.Chunks, Is.EqualTo(5));
            Assert.That(stats.Keyframes, Is.EqualTo(69 * 3 + 5 + 10));
        }

        [Test]
        public void ReplicaStats_CountsEachUnderrunOnce()
        {
            var stats = new ClientReplicaStats();
            stats.RecordFrame(0.15);
            stats.RecordFrame(-0.01);
            stats.RecordFrame(-0.02);
            stats.RecordFrame(0.1);
            stats.RecordFrame(-0.01);

            Assert.That(stats.Underruns, Is.EqualTo(2));
            Assert.That(stats.TimelineDelay, Is.EqualTo(-0.01).Within(TOLERANCE));
        }

        [Test]
        public void StatsRow_KeepsColumnOrderAcrossWindows()
        {
            var row = new NetworkStatsRow();
            row.Begin();
            row.Set("a", 1);
            row.Set("b", 2);
            row.End();

            row.Begin();
            row.Set("a", 3);
            Assert.Throws<InvalidOperationException>(() => row.Set("c", 4));

            row.Begin();
            row.Set("a", 5);
            Assert.Throws<InvalidOperationException>(() => row.End());
        }

        [Test]
        public void CsvWriter_WritesHeaderOnceAndLeavesMissingValuesEmpty()
        {
            var text = new StringWriter();
            var comments = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("role", "Client") };
            var writer = new NetworkStatsCsvWriter(text, comments);
            var row = new NetworkStatsRow();

            for (int second = 1; second <= 2; second++)
            {
                row.Begin();
                row.Set("fps", 60 * second);
                row.Set("rtt_ms", second == 1 ? double.NaN : 42.5);
                row.End();
                writer.Write(second, row);
            }

            string[] lines = text.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines, Is.EqualTo(new[] { "# role,Client", "time_s,fps,rtt_ms", "1.0,60,", "2.0,120,42.5" }));
        }

        [Test]
        public void StatusModel_StaysHiddenOffline()
        {
            var offline = new NetworkStatusModel(false);
            offline.SetVisible(true);
            Assert.That(offline.IsVisible, Is.False);

            var online = new NetworkStatusModel(true);
            online.SetVisible(true);
            Assert.That(online.IsVisible, Is.True);
        }

        [Test]
        public void TrafficCounter_CountsEveryCopyAndResetsPerWindow()
        {
            using var traffic = new NetworkTrafficCounter();
            traffic.RecordOut(typeof(EntityKeyframesMessage), 3, 100);
            traffic.RecordOut(typeof(EntityKeyframesMessage), 1, 50);
            traffic.RecordIn(typeof(EntityHealthMessage), 1, 20);

            Assert.That(traffic.OutMessages, Is.EqualTo(4));
            Assert.That(traffic.OutBytes, Is.EqualTo(350));
            Assert.That(traffic.InMessages, Is.EqualTo(1));
            Assert.That(traffic.InBytes, Is.EqualTo(20));
            MessageTraffic keyframes = traffic.Get(typeof(EntityKeyframesMessage));
            Assert.That(keyframes.OutMessages, Is.EqualTo(4));
            Assert.That(keyframes.OutBytes, Is.EqualTo(350));
            Assert.That(keyframes.InMessages, Is.Zero);

            traffic.ResetWindow();
            Assert.That(traffic.OutBytes, Is.Zero);
            Assert.That(traffic.InMessages, Is.Zero);
            Assert.That(keyframes.OutMessages, Is.Zero, "Per-type counters restart with the window too.");
        }

        [Test]
        public void SteamLinks_KeepWorstLinkAndSumTraffic()
        {
            var steam = new SteamLinkAggregate();
            var row = new NetworkStatsRow();
            steam.Reset();
            steam.Add(Status(ping: 40, quality: 0.99f, outBytes: 1000f, pending: 10));
            steam.Add(Status(ping: 120, quality: 0.9f, outBytes: 500f, pending: 300));
            row.Begin();
            steam.WriteTo(row);
            row.End();

            Assert.That(Value(row, "steam_links"), Is.EqualTo(2));
            Assert.That(Value(row, "steam_ping_ms"), Is.EqualTo(120));
            Assert.That(Value(row, "steam_quality_local"), Is.EqualTo(0.9).Within(TOLERANCE));
            Assert.That(Value(row, "steam_out_Bps"), Is.EqualTo(1500));
            Assert.That(Value(row, "steam_pending_reliable_B"), Is.EqualTo(300));

            steam.Reset();
            row.Begin();
            steam.WriteTo(row);
            row.End();
            Assert.That(Value(row, "steam_links"), Is.Zero);
            Assert.That(double.IsNaN(Value(row, "steam_ping_ms")), "Direct IP links leave Steam columns empty.");
        }

        [Test]
        public void SquadronDrift_ReportsMeanMaxAndSquadronsBeyondLoiter()
        {
            var drift = new SquadronDriftStats();
            Assert.That(double.IsNaN(drift.Mean));

            drift.Add(10f, 18f);
            drift.Add(50f, 18f);
            drift.Add(30f, 40f);

            Assert.That(drift.Count, Is.EqualTo(3));
            Assert.That(drift.Mean, Is.EqualTo(30.0).Within(TOLERANCE));
            Assert.That(drift.Max, Is.EqualTo(50f).Within(TOLERANCE));
            Assert.That(drift.BeyondLoiter, Is.EqualTo(1));

            drift.Reset();
            Assert.That(drift.Count, Is.Zero);
            Assert.That(drift.BeyondLoiter, Is.Zero);
        }

        [Test]
        public void PoolStats_SplitsShipAndFighterWaitsAndListsStarvingPools()
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            WeaponProfile profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(WeaponFamily.Torpedo),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool ship = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            ShotEffectPool fighter = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000, true);
            try
            {
                ship.Prewarm(1);
                fighter.Prewarm(1);
                Assert.That(ship.TryReserveSalvo(2), Is.False);
                Assert.That(fighter.TryReserveSalvo(3), Is.False);

                var stats = new ShotPoolStats();
                var row = new NetworkStatsRow();
                var details = new List<string>();
                row.Begin();
                stats.Collect(prewarmer.Caches, 1f, row, details);
                row.End();

                Assert.That(Value(row, "pool_waiting_shots"), Is.EqualTo(5));
                Assert.That(Value(row, "pool_ships_waiting"), Is.EqualTo(2));
                Assert.That(Value(row, "pool_fighters_waiting"), Is.EqualTo(3));
                Assert.That(Value(row, "pool_starving"), Is.EqualTo(2));
                Assert.That(stats.WaitingPools.Count, Is.EqualTo(2));
                Assert.That(stats.WaitingPools[0].IsFighters, Is.True, "Most waiting shots first.");
                Assert.That(details.Count, Is.EqualTo(2));

                ship.CancelReservation();
                fighter.CancelReservation();
                details.Clear();
                row.Begin();
                stats.Collect(prewarmer.Caches, 1f, row, details);
                row.End();
                Assert.That(Value(row, "pool_starving"), Is.Zero);
                Assert.That(stats.WaitingPools, Is.Empty);
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void StatsFiles_SecondInstanceInTheSameSecondGetsItsOwnFiles()
        {
            string folder = Path.Combine(Path.GetTempPath(), "EawNetStats_" + Guid.NewGuid().ToString("N"));
            var comments = new List<KeyValuePair<string, string>>();
            NetworkStatsFiles first = NetworkStatsFiles.Open(folder, "Battle_20261122_120000_Client", comments);
            NetworkStatsFiles second = NetworkStatsFiles.Open(folder, "Battle_20261122_120000_Client", comments);
            try
            {
                Assert.That(first.IsWriting && second.IsWriting);
                Assert.That(Path.GetFileName(second.StatsPath), Is.EqualTo("Battle_20261122_120000_Client_2_net.csv"));

                first.WritePool(1.0, new ShotPoolSample("Laser,Red", false, 4, 10, 12, 2, 0.5f, 0.25f));
                first.WriteLink(1.0, new NetworkLinkSample("Host", 42.0, false, default));
                first.Flush();
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }

            try
            {
                string[] pools = File.ReadAllLines(Path.Combine(folder, "Battle_20261122_120000_Client_pools.csv"));
                Assert.That(pools[1], Is.EqualTo("1.0,Laser Red,ships,4,10,12,2,500,250"));
                string[] links = File.ReadAllLines(Path.Combine(folder, "Battle_20261122_120000_Client_links.csv"));
                Assert.That(links[1].Split(',').Length, Is.EqualTo(links[0].Split(',').Length),
                    "A direct IP link still fills every column.");
            }
            finally { Directory.Delete(folder, true); }
        }

        [Test]
        public void StatsFiles_UncreatableFolderDisablesTheCsvWithOneError()
        {
            // A file where the folder should be: Directory.CreateDirectory fails with IOException.
            string blocker = Path.GetTempFileName();
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Battle network CSV disabled"));
                NetworkStatsFiles files = NetworkStatsFiles.Open(blocker, "Battle", new List<KeyValuePair<string, string>>());

                Assert.That(files.IsWriting, Is.False);
                Assert.That(files.StatsPath, Is.Null);
                var row = new NetworkStatsRow();
                row.Begin();
                row.Set("fps", 60);
                row.End();
                files.WriteStats(1.0, row);
                files.Dispose();
            }
            finally { File.Delete(blocker); }
        }

        [Test]
        public void ClientStats_KeepsTheSameColumnsBeforeHostStatsAndSquadronsArrive()
        {
            var stats = new ClientNetworkStats(new ClientReplicaBuffer(), new EntityLocator());
            var row = new NetworkStatsRow();
            var details = new List<string>();
            var links = new List<NetworkLinkSample>();

            for (int window = 0; window < 2; window++)
            {
                row.Begin();
                stats.Collect(row, details, links, new NetworkStatsWindow(1f, 60f, 10f));
                row.End();
            }

            Assert.That(Value(row, "squadrons"), Is.Zero);
            Assert.That(Value(row, "squadron_keyframes_s"), Is.Zero);
            Assert.That(double.IsNaN(Value(row, "squadron_drift_max_m")));
            Assert.That(double.IsNaN(Value(row, "host_fps")));
            Assert.That(links.Count, Is.EqualTo(2), "One host link per window.");
            Assert.That(links[0].Label, Is.EqualTo("Host"));
        }

        private static SteamNetConnectionRealTimeStatus_t Status(int ping, float quality, float outBytes, int pending) =>
            new SteamNetConnectionRealTimeStatus_t
            {
                m_nPing = ping,
                m_flConnectionQualityLocal = quality,
                m_flConnectionQualityRemote = quality,
                m_flOutBytesPerSec = outBytes,
                m_nSendRateBytesPerSecond = 100000,
                m_cbPendingReliable = pending
            };

        private static double Value(NetworkStatsRow row, string name)
        {
            for (int i = 0; i < row.Count; i++)
                if (row.GetName(i) == name) return row.GetValue(i);
            throw new ArgumentException($"No column '{name}'.");
        }
    }
}
