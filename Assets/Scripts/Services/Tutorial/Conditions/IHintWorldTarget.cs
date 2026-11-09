using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>A trigger that knows where it happened; the hint card marks that point.</summary>
    public interface IHintWorldTarget
    {
        bool TryGetWorldTarget(out Vector3 position);
    }
}
