namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsRouteNavigation
    {
        void Close();
        void SelectQualityPreset(int index);
        void ApplySettings();
    }
}
