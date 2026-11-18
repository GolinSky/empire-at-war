using System;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionInput
    {
        event Action SelectVisibleRequested;

        event Action SelectAllRequested;

        event Action<bool> SelectSquadronsRequested;

        event Action<bool> SelectShipsRequested;
    }
}
