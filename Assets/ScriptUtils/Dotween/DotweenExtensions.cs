using DG.Tweening;

namespace EmpireAtWar.ScriptUtils.Dotween
{
    public static class DotweenExtensions
    {
        public static bool KillIfExist(this Sequence sequence)
        {
            if (sequence != null && sequence.IsPlaying())
            {
                sequence.Kill();
                return true;
            }

            return false;
        }

        public static bool IsPlaying(this Sequence sequence)
        {
            return sequence != null && sequence.IsActive();
        }
    }
}