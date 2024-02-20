namespace Utils.TimerService
{
    public static class TimerFactory
    {
        public static ITimer ConstructTimer(float delay = 0f)
        {
            return new Timer(delay);
        }
    }
}