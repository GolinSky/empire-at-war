namespace EmpireAtWar.Services.Audio
{
    /// <summary>Coalesces firing requests without depending on Unity or allocating per shot.</summary>
    public sealed class WeaponAudioBudget
    {
        private const int MAX_VOICES_PER_SHIP = 2;
        private const int MAX_VOICES_PER_SOUND = 3;
        private const int MAX_STARTS_PER_WINDOW = 3;
        private const float START_WINDOW = 0.06f;

        private readonly object[] _owners;
        private readonly object[] _sounds;
        private readonly float[] _endsAt;
        private readonly bool[] _loops;
        private float _windowStart;
        private int _startsInWindow;

        public WeaponAudioBudget(int capacity)
        {
            _owners = new object[capacity];
            _sounds = new object[capacity];
            _endsAt = new float[capacity];
            _loops = new bool[capacity];
        }

        public float EndsAt(int voice) => _endsAt[voice];
        public bool IsOwnedBy(int voice, object owner) => ReferenceEquals(_owners[voice], owner);

        public int Request(object owner, object sound, bool loop, float duration, float now, out bool start)
        {
            start = false;
            int available = -1;
            int ownerCount = 0;
            int soundCount = 0;
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i] == null || _endsAt[i] <= now)
                {
                    if (available < 0) available = i;
                    continue;
                }

                bool sameOwner = ReferenceEquals(_owners[i], owner);
                bool sameSound = ReferenceEquals(_sounds[i], sound);
                if (sameOwner && sameSound)
                {
                    if (!loop || !_loops[i]) return -1;
                    _endsAt[i] = now + duration;
                    return i;
                }
                if (sameOwner) ownerCount++;
                if (sameSound) soundCount++;
            }

            if (now >= _windowStart + START_WINDOW)
            {
                _windowStart = now;
                _startsInWindow = 0;
            }
            if (available < 0 || ownerCount >= MAX_VOICES_PER_SHIP ||
                soundCount >= MAX_VOICES_PER_SOUND || _startsInWindow >= MAX_STARTS_PER_WINDOW)
                return -1;

            _owners[available] = owner;
            _sounds[available] = sound;
            _endsAt[available] = now + duration;
            _loops[available] = loop;
            _startsInWindow++;
            start = true;
            return available;
        }

        public void Release(int voice)
        {
            _owners[voice] = null;
            _sounds[voice] = null;
            _endsAt[voice] = 0f;
        }
    }
}
