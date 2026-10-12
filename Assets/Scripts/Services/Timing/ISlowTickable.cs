namespace EmpireAtWar.Services.Timing
{
    /// <summary>Work that only needs refreshing about once per <see cref="SlowTickService"/> interval.</summary>
    public interface ISlowTickable
    {
        void SlowTick();
    }
}
