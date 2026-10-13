namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        TUi CreateUi<TUi>() where TUi : BaseUi;

        void SetHudVisible(bool isVisible);
        void SetContentLayout(bool isFaction, bool isGroup);
        void SetContentVisible(bool isVisible);
    }
}
