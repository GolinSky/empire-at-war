using EmpireAtWar.Components.Selection.Marquee;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public interface IBattleMarker
    {
        long EntityId { get; }
        bool IsSquadron { get; }
        bool IsShown { get; }
        Rect ScreenRect { get; }
        int DrawOrder { get; }
        bool ContainsScreenPoint(Vector2 screenPoint);
        bool OverlapsMarquee(MarqueeRectangle rectangle);
    }
}
