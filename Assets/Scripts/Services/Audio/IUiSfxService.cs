namespace EmpireAtWar.Services.Audio
{
    public interface IUiSfxService
    {
        void RequestClick();
        void RequestSelect(UiSelectSfx kind);
    }
}
