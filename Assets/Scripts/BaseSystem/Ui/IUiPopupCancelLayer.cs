namespace EmpireAtWar.Ui.Base
{
    /// <summary>Cancel layer that receives every cancel request before focused screens.</summary>
    public interface IUiPopupCancelLayer
    {
        void SetPopupLayer(IUiCancelHandler handler);
    }
}
