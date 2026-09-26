namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISettingsPopupPresenter
    {
        void CloseSettings();
        void SelectQualityPreset(int index);
        void ApplySettings();
    }
}
