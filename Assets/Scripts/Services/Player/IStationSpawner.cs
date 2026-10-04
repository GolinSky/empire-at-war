namespace EmpireAtWar.Services.Player
{
    /// <summary>Spawns one player's space station; called once by the battle startup.</summary>
    public interface IStationSpawner
    {
        void Spawn();
    }
}
