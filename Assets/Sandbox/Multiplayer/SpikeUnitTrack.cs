namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Client-side keyframe history for one unit (ring buffer, oldest overwritten).
    public sealed class SpikeUnitTrack
    {
        private const int CAPACITY = 8;

        private readonly double[] _times = new double[CAPACITY];
        private readonly SpikeKinematics[] _states = new SpikeKinematics[CAPACITY];
        private int _count;
        private int _head;

        public bool HasSamples => _count > 0;

        public void Add(double time, SpikeKinematics state)
        {
            if (_count > 0 && time <= _times[Index(_count - 1)])
                return;

            if (_count == CAPACITY)
            {
                _head = (_head + 1) % CAPACITY;
                _count--;
            }

            int slot = Index(_count);
            _times[slot] = time;
            _states[slot] = state;
            _count++;
        }

        // Full snapshots: interpolate between the keyframes around renderTime.
        // Dead reckoning (or no later keyframe yet): extrapolate the latest keyframe at or before renderTime.
        public SpikeKinematics Sample(double renderTime, bool interpolate)
        {
            int before = -1;
            for (int i = 0; i < _count; i++)
            {
                if (_times[Index(i)] <= renderTime)
                    before = i;
            }

            if (before < 0)
                return _states[Index(0)];

            int from = Index(before);
            if (interpolate && before + 1 < _count)
            {
                int to = Index(before + 1);
                float t = (float)((renderTime - _times[from]) / (_times[to] - _times[from]));
                return SpikeKinematics.Lerp(_states[from], _states[to], t);
            }

            return _states[from].Extrapolate((float)(renderTime - _times[from]));
        }

        private int Index(int offset) => (_head + offset) % CAPACITY;
    }
}
