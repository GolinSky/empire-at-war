using EmpireAtWar.Components.Ship.Movement;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IShipEngineAudioObserver
    {
        MovementPhase Phase { get; }
        float Speed { get; }
    }
}
