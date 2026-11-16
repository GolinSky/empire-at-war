#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyDecisionDiagnostics
    {
        private float _elapsed;
        private int _windowCount;
        public ProfilerMarker Marker { get; }
        public int Count { get; private set; }
        public float PerSecond { get; private set; }

        public EnemyDecisionDiagnostics(string markerName)
        {
            Marker = new ProfilerMarker(markerName);
        }

        public void Record()
        {
            Count++;
            _windowCount++;
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < 1f) return;
            PerSecond = _windowCount / _elapsed;
            _windowCount = 0;
            _elapsed = 0f;
        }
    }
}
#endif
