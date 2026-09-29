using System;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Owns the single runtime copy of the input actions, so binding overrides apply in every scene.
    /// </summary>
    public sealed class InputActionsProvider : IDisposable
    {
        public GameInputActions Actions { get; } = new GameInputActions();

        public void Dispose()
        {
            Actions.Dispose();
        }
    }
}
