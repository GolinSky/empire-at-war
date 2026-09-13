namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveData
    {
        float Speed { get; }
        float Height { get; }
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
    }
}
