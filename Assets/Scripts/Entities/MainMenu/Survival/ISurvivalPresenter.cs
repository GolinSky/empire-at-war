using EmpireAtWar.Entities.Map;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public interface ISurvivalPresenter
    {
        void Close();
        void StartBattle();
        void SelectPlayer(int index);
        void SelectAlly(int index);
        void SelectEnemy(int index);
        void SelectMapSize(MapSize size);
        void SelectDifficulty(int index);
        void SelectSuperWeapons(bool enabled);
    }
}
