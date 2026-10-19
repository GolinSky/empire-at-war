using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Input;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Saves
{
    public sealed class BattleSaveInput : IInitializable, ILateDisposable
    {
        private readonly InputAction _save;
        private readonly InputAction _load;
        private readonly BattleSaveActions _actions;

        public BattleSaveInput(InputActionsProvider input, BattleSaveActions actions)
        {
            _save = input.Actions.asset.FindAction("Battle/QuickSave", true);
            _load = input.Actions.asset.FindAction("Battle/QuickLoad", true);
            _actions = actions;
        }

        public void Initialize()
        {
            _save.performed += Save;
            _load.performed += Load;
        }

        public void LateDispose()
        {
            _save.performed -= Save;
            _load.performed -= Load;
        }

        private void Save(InputAction.CallbackContext context) => _actions.Save(BattleSaveSlot.Quicksave);
        private void Load(InputAction.CallbackContext context) => _actions.QuickLoad();
    }
}
