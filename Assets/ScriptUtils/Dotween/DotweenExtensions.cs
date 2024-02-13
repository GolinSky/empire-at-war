using DG.Tweening;

namespace EmpireAtWar.ScriptUtils.Dotween
{
    public static class DotweenExtensions
    {
        public static bool KillIfExist(this Sequence sequence)
        {
            if (sequence != null && sequence.IsActive())
            {
                sequence.Kill();
                return true;
            }

            return false;
        }
    }
}