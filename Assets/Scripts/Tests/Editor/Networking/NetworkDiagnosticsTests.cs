using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Diagnostics;
using NUnit.Framework;
using UnityEngine;

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
    }
}
