namespace EmpireAtWar.Lifecycle
{
    public sealed class LifecycleTickGate
    {
        public bool IsTicking { get; private set; } = true;

        public void SetTicking(bool isTicking) => IsTicking = isTicking;
    }
}
