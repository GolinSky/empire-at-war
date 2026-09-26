namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISkirmishPopupPresenter
    {
        void CloseSkirmish();
        void StartGame();
        void SelectPlayerFaction(int index);
        void SelectEnemyFaction(int index);
        void SelectPlanet(int index);
        void SelectVictoryCondition(int index);
        void SelectEnemyDifficulty(int index);
        void SelectStartingMoney(float amount);
    }
}
