using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met after a tooltip with Strong against / Weak against rows stayed open long enough to read.</summary>
    public sealed class MatchupTooltipHintCondition : IPolledHintCondition
    {
        private const float REQUIRED_SECONDS = 1.5f;

        private readonly ITooltipModelObserver _tooltip;
        private float _seconds;

        public bool IsMet => _seconds >= REQUIRED_SECONDS;

        public MatchupTooltipHintCondition(ITooltipModelObserver tooltip)
        {
            _tooltip = tooltip;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            TooltipContent content = _tooltip.Content;
            bool hasMatchups = _tooltip.IsVisible && content != null &&
                               (content.StrongAgainst.Count > 0 || content.WeakAgainst.Count > 0);
            _seconds = hasMatchups ? _seconds + deltaTime : 0f;
        }

        public void Dispose()
        {
        }
    }
}
