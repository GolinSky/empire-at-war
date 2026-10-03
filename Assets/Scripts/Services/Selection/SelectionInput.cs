using System;
using EmpireAtWar.Services.Input;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SelectionInput : ISelectionInput, ITickable
    {
        private readonly GameInputActions.BattleActions _battle;

        public event Action SelectVisibleRequested;

        public event Action SelectAllRequested;

        public SelectionInput(InputActionsProvider provider)
        {
            _battle = provider.Actions.Battle;
        }

        public void Tick()
        {
            // Ctrl+Shift+A also satisfies the Ctrl+A shortcut, so the longer one is checked first.
            if (_battle.SelectAll.WasPerformedThisFrame())
            {
                SelectAllRequested?.Invoke();
            }
            else if (_battle.SelectVisible.WasPerformedThisFrame())
            {
                SelectVisibleRequested?.Invoke();
            }
        }
    }
}
