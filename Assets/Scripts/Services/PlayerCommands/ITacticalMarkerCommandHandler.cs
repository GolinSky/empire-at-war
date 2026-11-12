namespace EmpireAtWar.Services.PlayerCommands
{
    public interface ITacticalMarkerCommandHandler
    {
        bool TryPlace(PlaceMarkerCommand command);
    }
}
