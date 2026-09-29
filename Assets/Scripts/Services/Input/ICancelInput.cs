using System;

namespace EmpireAtWar.Services.Input
{
    public interface ICancelInput
    {
        event Action CancelPressed;
    }
}
