#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace EmpireAtWar.Services.Timing
{
    internal sealed class BattleHitchCapture
    {
        private const int CONTEXT_FRAMES = 30;
        private const int WRITE_BATCH_SIZE = 64;

        private readonly string _path;
        private readonly string[] _metricNames;
        private readonly string[] _workloadNames;
        private readonly int _firstMarker;
        private readonly long _unavailable;
        private readonly BattleCaptureFrame[] _before;
        private BattleCaptureFrame[] _batch;
        private BattleCaptureFrame[] _writing;
        private Task _write = Task.CompletedTask;
        private int _nextBefore;
        private int _beforeCount;
        private int _afterRemaining;
        private int _batchCount;
        private long _writeWaitTicks;
        private bool _failed;

        public BattleHitchCapture(string path, string[] metricNames, string[] workloadNames,
            int firstMarker, long unavailable)
        {
            _path = path;
            _metricNames = metricNames;
            _workloadNames = workloadNames;
            _firstMarker = firstMarker;
            _unavailable = unavailable;
            _before = CreateFrames(CONTEXT_FRAMES);
            _batch = CreateFrames(WRITE_BATCH_SIZE);
            _writing = CreateFrames(WRITE_BATCH_SIZE);
        }

        public void Observe(BattleCaptureFrame frame)
        {
            if (_write.IsFaulted && !_failed) WaitForWrite();
            if (_failed) return;
            // Waiting after the previous observation contributes to this completed frame's duration.
            frame.CaptureWriteWaitMs = _writeWaitTicks * 1000d / Stopwatch.Frequency;
            _writeWaitTicks = 0;
            if (frame.IsHitch)
            {
                // The ring contains only frames not already included in a previous hitch's tail.
                int oldest = (_nextBefore - _beforeCount + CONTEXT_FRAMES) % CONTEXT_FRAMES;
                for (int i = 0; i < _beforeCount; i++)
                    Append(_before[(oldest + i) % CONTEXT_FRAMES]);
                _beforeCount = 0;
                Append(frame);
                _afterRemaining = CONTEXT_FRAMES;
            }
            else if (_afterRemaining > 0)
            {
                Append(frame);
                if (--_afterRemaining == 0) Flush();
            }
            else
            {
                _before[_nextBefore].CopyFrom(frame);
                _nextBefore = (_nextBefore + 1) % CONTEXT_FRAMES;
                _beforeCount = Math.Min(_beforeCount + 1, CONTEXT_FRAMES);
            }
        }

        public void FinishWindow()
        {
            _beforeCount = 0;
            _afterRemaining = 0;
            Flush();
            WaitForWrite();
            _writeWaitTicks = 0;
        }

        private BattleCaptureFrame[] CreateFrames(int count)
        {
            var frames = new BattleCaptureFrame[count];
            for (int i = 0; i < count; i++)
                frames[i] = new BattleCaptureFrame(_metricNames.Length, _metricNames.Length - _firstMarker, _workloadNames.Length);
            return frames;
        }

        private void Append(BattleCaptureFrame frame)
        {
            if (_failed) return;
            _batch[_batchCount++].CopyFrom(frame);
            if (_batchCount == WRITE_BATCH_SIZE) Flush();
        }

        private void Flush()
        {
            if (_batchCount == 0) return;
            WaitForWrite();
            if (_failed) return;
            (_writing, _batch) = (_batch, _writing);
            var batch = _writing;
            int count = _batchCount;
            _batchCount = 0;
            _write = Task.Run(() => WriteBatch(batch, count));
        }

        private void WaitForWrite()
        {
            if (_failed) return;
            long start = _write.IsCompleted ? 0 : Stopwatch.GetTimestamp();
            try
            {
                _write.GetAwaiter().GetResult();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                _failed = true;
                Debug.LogError($"Hitch capture stopped writing {_path}: {e.Message}");
            }
            finally
            {
                if (start != 0) _writeWaitTicks += Stopwatch.GetTimestamp() - start;
            }
        }

        private void WriteBatch(BattleCaptureFrame[] frames, int count)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            using var writer = new StreamWriter(_path, true);
            if (writer.BaseStream.Length == 0)
                BattlePerformanceCaptureReport.WriteCsvHeader(writer, "frame,seconds,is_hitch,capture_write_wait_ms", _metricNames, _workloadNames, _firstMarker);
            for (int i = 0; i < count; i++)
            {
                var frame = frames[i];
                writer.Write(frame.UnityFrame.ToString(CultureInfo.InvariantCulture));
                writer.Write(',');
                writer.Write(frame.RealtimeSeconds.ToString("F6", CultureInfo.InvariantCulture));
                writer.Write(frame.IsHitch ? ",1" : ",0");
                writer.Write(',');
                writer.Write(frame.CaptureWriteWaitMs.ToString("F3", CultureInfo.InvariantCulture));
                BattlePerformanceCaptureReport.WriteCsvCounters(writer, 0, frame.Metrics, frame.MarkerCounts,
                    frame.Workload, _metricNames.Length, _workloadNames.Length, _firstMarker, _unavailable);
            }
        }
    }
}
#endif
