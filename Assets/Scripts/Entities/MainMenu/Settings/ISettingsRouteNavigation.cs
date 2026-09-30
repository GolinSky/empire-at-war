namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsRouteNavigation
    {
        void Close();
        void ApplySettings();
        void DiscardChanges();
        void ResetToDefaults();

        void SelectQualityPreset(int index);
        void SelectWindowMode(int index);
        void SelectResolution(int index);
        void SelectFrameRateLimit(int index);
        void SetVSync(bool isOn);

        void SetPanSpeed(float multiplier);
        void SetZoomSpeed(float multiplier);
        void SetEdgeScrolling(bool isOn);
        void SetInvertZoom(bool isOn);

        void StartRebind(int row);
        void ResetBinding(int row);

        void ChoosePromptAction(SettingsPromptAction action);
    }
}
