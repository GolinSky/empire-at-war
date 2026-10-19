namespace EmpireAtWar.Entities.Menu
{
    public interface IPauseMenuPresenter
    {
        void Close();

        void OpenSettings();

        void ExitSkirmish();
        void SaveNamed(string name);
        void QuickSave();
        void QuickLoad();
    }
}
