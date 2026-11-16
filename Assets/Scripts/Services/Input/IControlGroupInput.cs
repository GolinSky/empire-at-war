using System;

namespace EmpireAtWar.Services.Input
{
    public interface IControlGroupInput
    {
        event Action<int, bool> SlotRequested;
        bool IsPreviewHeld { get; }
    }
}
