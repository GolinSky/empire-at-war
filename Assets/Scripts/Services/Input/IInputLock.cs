using System;

namespace EmpireAtWar.Services.Input
{
    public interface IInputLock
    {
        event Action<bool> LockChanged;

        bool IsLocked { get; }

        /// <summary>Disables camera and battle input until every returned handle is disposed.</summary>
        IDisposable Acquire();
    }
}
