using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Views.SpawnArea
{
    /// <summary>Shows where a team may deploy reinforcements while a placement is running.</summary>
    public interface ISpawnAreaOverlay
    {
        void Show(PlayerId team, UnitRequest request, Func<Vector3, bool> isPositionValid);

        Vector3 GetPlacementPosition(Vector3 position);

        float GetPlacementSpacing(float minimum);

        void Hide();
    }
}
