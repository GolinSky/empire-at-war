namespace EmpireAtWar.Services.TacticalMarkers
{
    public static class TacticalMarkerTarget
    {
        public const long NO_TARGET = -1;
        public static long SiteId(int siteIndex) => -2L - siteIndex;
        public static bool IsSite(long targetId) => targetId <= -2 && targetId >= -2L - int.MaxValue;
        public static int SiteIndex(long targetId) => checked((int)(-2L - targetId));
    }
}
