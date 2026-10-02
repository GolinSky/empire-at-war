namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveData
    {
        float Speed { get; }
        float Height { get; }
        float HullBottom { get; }
        float HullTop { get; }
        float RotationSpeed { get; }
        float TurnAcceleration { get; }
        float HyperSpaceDuration { get; }
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
    }
}
