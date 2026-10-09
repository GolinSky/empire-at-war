namespace EmpireAtWar.Ui.Base
{
    /// <summary>
    /// Base of every UI controller. A controller that should react to cancel (Escape) focuses itself
    /// while its UI is active and overrides <see cref="HandleCancel"/>.
    /// </summary>
    public abstract class UiController : IUiCancelHandler
    {
        private readonly IUiCancelRouter _cancelRouter;

        protected IUiService UiService { get; }

        public bool IsFocused { get; private set; }

        protected UiController(IUiService uiService, IUiCancelRouter cancelRouter)
        {
            UiService = uiService;
            _cancelRouter = cancelRouter;
        }

        public bool TryCancel()
        {
            return IsFocused && HandleCancel();
        }

        /// <summary>Returns true when the cancel request was consumed.</summary>
        protected virtual bool HandleCancel()
        {
            return false;
        }

        protected void Focus()
        {
            IsFocused = true;
            _cancelRouter.Focus(this);
        }

        protected void Unfocus()
        {
            if (!IsFocused)
            {
                return;
            }

            IsFocused = false;
            _cancelRouter.Unfocus(this);
        }
    }
}
