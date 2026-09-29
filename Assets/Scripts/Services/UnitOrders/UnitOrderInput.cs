using System;
using EmpireAtWar.Services.Input;
using UnityEngine.InputSystem;
using Zenject;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class UnitOrderInput : IUnitOrderInput, IInitializable, IDisposable
    {
        private readonly GameInputActions.BattleActions _battle;

        public event Action QueueWaypointReleased;

        public bool IsQueueWaypointHeld => _battle.QueueWaypoint.IsPressed();

        public UnitOrderInput(InputActionsProvider provider)
        {
            _battle = provider.Actions.Battle;
        }

        public void Initialize()
        {
            _battle.QueueWaypoint.canceled += HandleQueueWaypointCanceled;
        }

        public void Dispose()
        {
            _battle.QueueWaypoint.canceled -= HandleQueueWaypointCanceled;
        }

        private void HandleQueueWaypointCanceled(InputAction.CallbackContext context)
        {
            QueueWaypointReleased?.Invoke();
        }
    }
}
