namespace EmpireAtWar.Lifecycle
{
    /// <summary>Runs after registered Tick and MonoBehaviour Update callbacks, before LateTick; order is scope-local.</summary>
    public interface IPostTickable
    {
        void PostTick();
    }
}
