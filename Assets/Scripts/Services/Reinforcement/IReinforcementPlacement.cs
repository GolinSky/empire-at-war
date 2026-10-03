using EmpireAtWar.Views.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>The rules for placing one kind of reinforcement: its preview, where it may go and how it spawns.</summary>
    public interface IReinforcementPlacement
    {
        UnitSpawnView CreatePreview();

        bool IsPositionValid(Vector3 position);

        void Spawn(Vector3 position);
    }
}
