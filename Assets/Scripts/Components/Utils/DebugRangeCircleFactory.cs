using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    // Creates debug range rings sharing one serialized line material.
    public sealed class DebugRangeCircleFactory
    {
        private readonly IRangeDebugObserver _rangeDebug;

        private readonly Material _lineMaterial;

        public DebugRangeCircleFactory(IRangeDebugObserver rangeDebug, Material lineMaterial)
        {
            _lineMaterial = lineMaterial;
            _rangeDebug = rangeDebug;
        }

        public DebugRangeCircle Create(string name, Color color, ISelectionModelObserver selection)
        {
            return new DebugRangeCircle(name: name, color: color, lineMaterial: _lineMaterial, rangeDebug: _rangeDebug, selection: selection);
        }
    }
}
