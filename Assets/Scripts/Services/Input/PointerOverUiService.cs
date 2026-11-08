namespace EmpireAtWar.Services.Input
{
    public sealed class PointerOverUiService : IPointerOverUi
    {
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _uiHitTest;

        // Before the first pointer report the position is not where the cursor is, so it cannot be over UI.
        public bool IsPointerOverUi => _pointerInput.HasPosition && _uiHitTest.IsOverUi(_pointerInput.Position);

        public PointerOverUiService(IPointerInput pointerInput, IUiHitTest uiHitTest)
        {
            _pointerInput = pointerInput;
            _uiHitTest = uiHitTest;
        }
    }
}
