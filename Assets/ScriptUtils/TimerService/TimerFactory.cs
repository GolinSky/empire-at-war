namespace Utils.TimerService
{
    public static class TimerFactory
    {
        public static ITimer ConstructTimer(float delay)
        {
            return new Timer(delay);
        }
    }
}