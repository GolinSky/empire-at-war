using System;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Project-lifetime recovery presentation shared with scene loading without depending on game commands.</summary>
    public sealed class OnlineRecoveryProgress
    {
        public bool IsActive { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public event Action Changed;

        public void Set(bool isActive, string status)
        {
            if (IsActive == isActive && Status == status) return;
            IsActive = isActive;
            Status = status;
            Changed?.Invoke();
        }
    }
}
