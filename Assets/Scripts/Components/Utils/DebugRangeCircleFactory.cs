using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    // Creates debug range rings sharing one serialized line material.
    public sealed class DebugRangeCircleFactory
    {
        private readonly Material _lineMaterial;
        private readonly IRangeDebugObserver _rangeDebug;

        public DebugRangeCircleFactory(Material lineMaterial, IRangeDebugObserver rangeDebug)
        {
            _lineMaterial = lineMaterial;
            _rangeDebug = rangeDebug;
        }

        public DebugRangeCircle Create(string name, Color color, ISelectionModelObserver selection)
        {
            return new DebugRangeCircle(name, color, _lineMaterial, _rangeDebug, selection);
        }
    }
}
