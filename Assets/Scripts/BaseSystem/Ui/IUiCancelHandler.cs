namespace EmpireAtWar.Ui.Base
{
    public interface IUiCancelHandler
    {
        /// <summary>Returns true when the cancel request was consumed.</summary>
        bool TryCancel();
    }
}
