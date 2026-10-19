namespace EmpireAtWar.Entities.Saves
{
    public interface ISaveBrowserPresenter
    {
        void Close();
        void SelectMode(int index);
        void SelectSave(int index);
        void SaveAsNew();
        void ConfirmName(string name);
        void CancelName();
    }
}
