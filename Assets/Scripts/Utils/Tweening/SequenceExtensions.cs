using DG.Tweening;

namespace EmpireAtWar.Utils
{
    public static class SequenceExtensions
    {
        public static bool KillExt(this Sequence sequence)
        {
            sequence?.Kill();
            return sequence != null;
        }
    }
}