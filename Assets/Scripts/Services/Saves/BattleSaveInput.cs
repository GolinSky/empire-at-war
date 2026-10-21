using EmpireAtWar.Lifecycle;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Input;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Saves
{
    public sealed class BattleSaveInput : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private readonly InputAction _save;
        private readonly InputAction _load;
        private readonly BattleSaveActions _actions;
        private readonly INotifier<BattleState> _battleState;
        private readonly IInputLock _inputLock;
        private bool _hasEnded;

        public BattleSaveInput(InputActionsProvider input, BattleSaveActions actions,
            INotifier<BattleState> battleState, IInputLock inputLock)
        {
            _save = input.Actions.asset.FindAction("Battle/QuickSave", true);
            _load = input.Actions.asset.FindAction("Battle/QuickLoad", true);
            _actions = actions;
            _battleState = battleState;
            _inputLock = inputLock;
        }

        public void Initialize()
        {
            _save.performed += Save;
            _load.performed += Load;
            _inputLock.LockChanged += HandleLockChanged;
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _save.performed -= Save;
            _load.performed -= Load;
            _inputLock.LockChanged -= HandleLockChanged;
            _battleState.RemoveObserver(this);
            _load.Disable();
        }

        public void UpdateState(BattleState state)
        {
            _hasEnded = state == BattleState.Ended;
            EnableEndedBattleLoad();
        }

        private void HandleLockChanged(bool isLocked) => EnableEndedBattleLoad();

        private void EnableEndedBattleLoad()
        {
            // Ending locks the Battle map. Keep only reload available, whichever observer ran first.
            if (_hasEnded) _load.Enable();
        }

        private void Save(InputAction.CallbackContext context) => _actions.Save(BattleSaveSlot.Quicksave);
        private void Load(InputAction.CallbackContext context) => _actions.QuickLoad();
    }
}
