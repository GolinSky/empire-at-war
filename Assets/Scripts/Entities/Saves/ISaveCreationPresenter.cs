namespace EmpireAtWar.Entities.Saves
{
    /// <summary>Save-as-new and naming actions; only the save route offers them.</summary>
    public interface ISaveCreationPresenter
    {
        void SaveAsNew();
        void ConfirmName(string name);
        void CancelName();
    }
}
