using System;
using EmpireAtWar.Services.Input;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SelectionInput : ISelectionInput, ITickable
    {
        private readonly GameInputActions.BattleActions _battle;

        public event Action SelectVisibleRequested;

        public event Action SelectAllRequested;

        public event Action<bool> SelectSquadronsRequested;

        public event Action<bool> SelectShipsRequested;

        public bool IsAddToSelectionHeld => _battle.AddToSelection.IsPressed();

        public SelectionInput(InputActionsProvider provider)
        {
            _battle = provider.Actions.Battle;
        }

        public void Tick()
        {
            // For each key, the map-wide chord also satisfies the visible chord.
            if (_battle.SelectAll.enabled && _battle.SelectAll.WasPerformedThisFrame())
            {
                SelectAllRequested?.Invoke();
            }
            else if (_battle.SelectVisible.enabled && _battle.SelectVisible.WasPerformedThisFrame())
            {
                SelectVisibleRequested?.Invoke();
            }
            else if (_battle.SelectAllSquadrons.enabled && _battle.SelectAllSquadrons.WasPerformedThisFrame())
            {
                SelectSquadronsRequested?.Invoke(false);
            }
            else if (_battle.SelectVisibleSquadrons.enabled && _battle.SelectVisibleSquadrons.WasPerformedThisFrame())
            {
                SelectSquadronsRequested?.Invoke(true);
            }
            else if (_battle.SelectAllShips.enabled && _battle.SelectAllShips.WasPerformedThisFrame())
            {
                SelectShipsRequested?.Invoke(false);
            }
            else if (_battle.SelectVisibleShips.enabled && _battle.SelectVisibleShips.WasPerformedThisFrame())
            {
                SelectShipsRequested?.Invoke(true);
            }
        }
    }
}
