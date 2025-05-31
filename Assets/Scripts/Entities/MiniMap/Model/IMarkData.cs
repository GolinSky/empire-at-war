using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public interface IMarkData
    {
        Vector3 Position { get; }
        Sprite Icon { get; }
    }
}