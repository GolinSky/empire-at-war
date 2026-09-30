using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Explicit("Requires the local 2026-09-30 Render Audit recording.")]
    public sealed class RenderAuditProfilerToolTests
    {
        private string _backupPath;

        [SetUp]
        public void LoadRecording()
        {
            var recording = Path.Combine(Application.dataPath,
                "../Logs/RenderAudit/20260930T165740445Z-capture/profiler.data");
            Assert.That(File.Exists(recording), Is.True, "The regression recording must be present.");
            _backupPath = Path.Combine(Path.GetTempPath(), "render-audit-tests-" + Guid.NewGuid() + ".data");
            Assert.That(ProfilerDriver.SaveProfile(_backupPath), Is.True);
            Assert.That(ProfilerDriver.LoadProfile(recording, false), Is.True);
        }

        [TearDown]
        public void RestoreRecording()
        {
            if (File.Exists(_backupPath))
            {
                Assert.That(ProfilerDriver.LoadProfile(_backupPath, false), Is.True);
                File.Delete(_backupPath);
            }
        }

        [Test]
        public void FrameSummaries_ExportRecordingContainingUnnamedSamples()
        {
            var frames = Enumerable.Range(ProfilerDriver.firstFrameIndex,
                ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1).ToArray();
            var unnamedSamples = 0;
            foreach (var frame in frames)
            {
                for (var thread = 0; ; thread++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(frame, thread);
                    if (!view.valid) break;
                    for (var sample = 0; sample < view.sampleCount; sample++)
                        if (view.GetSampleName(sample) == null) unnamedSamples++;
                }
            }

            Assert.That(unnamedSamples, Is.GreaterThan(0), "The fixture must reproduce unnamed samples.");
            var operation = typeof(RenderAuditProfilerTool).GetNestedType("CaptureOperation", BindingFlags.NonPublic);
            var method = operation.GetMethod("CaptureFrameSummaries", BindingFlags.Static | BindingFlags.NonPublic);
            var summaries = JArray.FromObject(method.Invoke(null, new object[] { frames }));

            Assert.That(summaries.Count, Is.EqualTo(2));
            Assert.That(summaries.All(frame => ((JArray)frame["threads"]).Count > 0), Is.True);
            Assert.That(summaries.SelectMany(frame => frame["threads"])
                .SelectMany(thread => thread["cpu_hotspots"])
                .All(marker => marker["marker_id"].Type == JTokenType.Integer), Is.True);
        }

        [Test]
        public void CpuHotspots_PreserveMarkerIdentityCallCountsAndInclusiveTime()
        {
            var operation = typeof(RenderAuditProfilerTool).GetNestedType("CaptureOperation", BindingFlags.NonPublic);
            var method = operation.GetMethod("CaptureCpuHotspots", BindingFlags.Static | BindingFlags.NonPublic);
            var exportedMarkers = 0;
            for (var thread = 0; ; thread++)
            {
                using var view = ProfilerDriver.GetRawFrameDataView(ProfilerDriver.firstFrameIndex, thread);
                if (!view.valid) break;
                var markers = JArray.FromObject(method.Invoke(null, new object[] { view }));
                Assert.That(markers.Count, Is.LessThanOrEqualTo(40));
                Assert.That(markers.Select(marker => (int)marker["marker_id"]).Distinct().Count(), Is.EqualTo(markers.Count));
                foreach (var marker in markers)
                {
                    var samples = Enumerable.Range(0, view.sampleCount)
                        .Where(sample => view.GetSampleMarkerId(sample) == (int)marker["marker_id"]).ToArray();
                    Assert.That((int)marker["calls"], Is.EqualTo(samples.Length));
                    Assert.That((double)marker["inclusive_ms"],
                        Is.EqualTo(samples.Sum(sample => (double)view.GetSampleTimeMs(sample))).Within(0.000001));
                    Assert.That((string)marker["marker"], Is.EqualTo(view.GetSampleName(samples[0])));
                    Assert.That((double)marker["self_ms"], Is.GreaterThanOrEqualTo(0));
                    exportedMarkers++;
                }
            }
            Assert.That(exportedMarkers, Is.GreaterThan(0));
        }
    }
}
