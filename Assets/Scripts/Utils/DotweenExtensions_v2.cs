using DG.Tweening;

namespace EmpireAtWar.Utils
{
    public static class DotweenExtensions_v2
    {
        public static bool KillExt(this Sequence sequence)
        {
            sequence?.Kill();
            return sequence != null;
        }
    }
}