using System.Collections.Generic;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>Hull raycast result for one weapon hardpoint: blocked flag per degree and the clear arcs.</summary>
    public sealed class HardPointArcProbeResult
    {
        public HardPointArcProbeResult(bool[] blocked, IReadOnlyList<YawArc> clearArcs)
        {
            Blocked = blocked;
            ClearArcs = clearArcs;
        }

        /// <summary>Index i is yaw <see cref="HardPointArcCalculator.YawOf"/>(i).</summary>
        public IReadOnlyList<bool> Blocked { get; }
        public IReadOnlyList<YawArc> ClearArcs { get; }

        /// <summary>Every ray hits hull: the hardpoint is buried in overlapping geometry.</summary>
        public bool IsInconclusive => ClearArcs.Count == 0;
        public YawArc Suggested => ClearArcs[0];
        public bool HasSecondArc =>
            ClearArcs.Count > 1 && ClearArcs[1].Width >= HardPointArcCalculator.SECOND_ARC_MIN_DEGREES;
        public YawArc SecondArc => ClearArcs[1];
    }
}
