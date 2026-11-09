using System;

namespace EmpireAtWar.Entities.MiniMap
{
    /// <summary>Reports minimap input the player completed; read-only.</summary>
    public interface IMiniMapInputObserver
    {
        event Action CameraMoved;
        event Action MoveOrdered;
    }
}
