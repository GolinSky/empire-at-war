using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Radar
{
    public interface IRadarScanner
    {
        PlayerId Owner { get; }

        float Range { get; }

        float Delay { get; }

        Vector3 ScanCenter { get; }

        void ApplyScan(IReadOnlyList<IEntity> detectedEnemies);
    }
}
