namespace EmpireAtWar.Entities.MainMenu.Play
{
    public interface IPlayPresenter
    {
        void OpenSkirmish();
        void OpenSurvival();
        void OpenSimulation();
        void Close();
        void SelectSaveMode(int index);
        void SelectSave(int index);
        void LoadSave();
    }
}
