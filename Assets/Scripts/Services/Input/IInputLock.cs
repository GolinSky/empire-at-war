using System;

namespace EmpireAtWar.Services.Input
{
    public interface IInputLock
    {
        event Action<bool> LockChanged;

        bool IsLocked { get; }

        /// <summary>Disables camera and battle input until every returned handle is disposed.</summary>
        IDisposable Acquire();

        /// <summary>Disables battle input only; the camera stays movable. Does not change <see cref="IsLocked"/>.</summary>
        IDisposable AcquireBattle();
    }
}
