using System.Numerics;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public readonly struct CinematicSelection
    {
        public CinematicCandidate Target { get; }
        public Vector3 FocusOffset { get; }

        public CinematicSelection(CinematicCandidate target, Vector3 focusOffset)
        {
            Target = target;
            FocusOffset = focusOffset;
        }
    }
}
