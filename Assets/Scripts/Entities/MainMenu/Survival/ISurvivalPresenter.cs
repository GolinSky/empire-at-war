namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public interface ISurvivalPresenter
    {
        void Close();
        void SelectPlayer(int index);
        void SelectAlly(int index);
        void SelectEnemy(int index);
        void SelectMapSize(int index);
        void SelectDifficulty(int index);
        void SelectSuperWeapons(bool enabled);
    }
}
