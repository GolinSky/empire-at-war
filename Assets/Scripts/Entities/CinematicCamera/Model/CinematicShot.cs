namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public readonly struct CinematicShot
    {
        public CinematicShotType Type { get; }
        public float Side { get; }
        public float Duration { get; }

        public CinematicShot(CinematicShotType type, float side, float duration)
        {
            Type = type;
            Side = side;
            Duration = duration;
        }
    }
}
