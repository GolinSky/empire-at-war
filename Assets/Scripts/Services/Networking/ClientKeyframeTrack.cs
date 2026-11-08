namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Client-side keyframe history for one entity. Sampling extrapolates the latest keyframe at or before the
    /// render time; once the next keyframe is known the two are blended so corrections do not snap.
    /// </summary>
    public sealed class ClientKeyframeTrack
    {
        private const int CAPACITY = 8;

        private readonly double[] _times = new double[CAPACITY];
        private readonly EntityMotion[] _motions = new EntityMotion[CAPACITY];
        private int _count;
        private int _head;

        public bool HasSamples => _count > 0;

        public void Add(double time, EntityMotion motion)
        {
            if (_count > 0 && time <= _times[Index(_count - 1)]) return;
            if (_count == CAPACITY)
            {
                _head = (_head + 1) % CAPACITY;
                _count--;
            }

            int slot = Index(_count);
            _times[slot] = time;
            _motions[slot] = motion;
            _count++;
        }

        public EntityMotion Sample(double renderTime)
        {
            int before = -1;
            for (int i = 0; i < _count; i++)
                if (_times[Index(i)] <= renderTime) before = i;
            if (before < 0) return _motions[Index(0)];

            int from = Index(before);
            EntityMotion predicted = _motions[from].Extrapolate((float)(renderTime - _times[from]));
            if (before + 1 >= _count) return predicted;

            int to = Index(before + 1);
            float t = (float)((renderTime - _times[from]) / (_times[to] - _times[from]));
            EntityMotion target = _motions[to].Extrapolate((float)(renderTime - _times[to]));
            return EntityMotion.Lerp(predicted, target, t);
        }

        private int Index(int offset) => (_head + offset) % CAPACITY;
    }
}
