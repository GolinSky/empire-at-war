using UnityEngine;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiPlacement
    {
        Transform GetParent<TUi>() where TUi : BaseUi;
        Transform ContextualSlot { get; }
        UiContentLayout FactionLayout { get; }
    }
}
