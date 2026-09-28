namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public interface ISkirmishRouteNavigation
    {
        void Close();
        void StartGame();
        void SelectSlotOccupant(int slotIndex, int occupantIndex);
        void SelectSlotFaction(int slotIndex, int factionIndex);
        void SelectSlotTeam(int slotIndex, int teamIndex);
        void SelectSlotColor(int slotIndex, int colorIndex);
        void SelectPlanet(int index);
        void SelectMapSize(int index);
        void SelectVictoryCondition(int index);
        void SelectStartingMoney(float amount);
    }
}
