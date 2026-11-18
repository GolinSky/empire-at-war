#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace EmpireAtWar.Services.Timing
{
    internal struct BattleSystemSample
    {
        public string Name;
        public long Nanoseconds;
        public long Bytes;
        public long Calls;
    }
}
#endif
