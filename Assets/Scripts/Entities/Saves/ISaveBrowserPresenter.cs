namespace EmpireAtWar.Entities.Saves
{
    public interface ISaveBrowserPresenter
    {
        void Close();
        void SelectSave(int index);
        void RemoveSave(int index);
    }
}
