namespace EmpireAtWar.Services.Popups
{
    public interface IPopupService
    {
        PopupHandle Open(IPopupController controller);
        void Close(PopupHandle handle);
        bool IsOpen(PopupHandle handle);
    }
}
