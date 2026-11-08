using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host-side error between the clients' dead-reckoning prediction and the real sim, per unit kind.
    public sealed class SpikeDeadReckoningStats
    {
        private readonly float[] _maxError = new float[2];
        private readonly double[] _errorSum = new double[2];
        private readonly int[] _samples = new int[2];
        private int _keyframes;
        private int _ticks;

        public float MaxError(SpikeUnitKind kind) => _maxError[(int)kind];
        public float MeanError(SpikeUnitKind kind) => _samples[(int)kind] == 0 ? 0f : (float)(_errorSum[(int)kind] / _samples[(int)kind]);
        public float KeyframesPerTick => _ticks == 0 ? 0f : _keyframes / (float)_ticks;

        public void RecordError(SpikeUnitKind kind, float error)
        {
            int index = (int)kind;
            _maxError[index] = Mathf.Max(_maxError[index], error);
            _errorSum[index] += error;
            _samples[index]++;
        }

        public void RecordKeyframes(int count)
        {
            _keyframes += count;
            _ticks++;
        }

        public void Reset()
        {
            System.Array.Clear(_maxError, 0, _maxError.Length);
            System.Array.Clear(_errorSum, 0, _errorSum.Length);
            System.Array.Clear(_samples, 0, _samples.Length);
            _keyframes = 0;
            _ticks = 0;
        }
    }
}
