using System.Globalization;

namespace EmpireAtWar.Entities.Game
{
    internal static class BattleDuration
    {
        private const int SECONDS_PER_MINUTE = 60;
        private const int MINUTES_PER_HOUR = 60;
        private const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * MINUTES_PER_HOUR;

        public static string Format(int seconds) => string.Format(CultureInfo.InvariantCulture,
            "{0}:{1:00}:{2:00}", seconds / SECONDS_PER_HOUR,
            seconds / SECONDS_PER_MINUTE % MINUTES_PER_HOUR, seconds % SECONDS_PER_MINUTE);
    }
}
