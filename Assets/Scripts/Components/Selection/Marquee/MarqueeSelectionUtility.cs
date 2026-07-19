using System;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Selection.Marquee
{
    public static class MarqueeSelectionUtility
    {
        public static void CollectInside<T>(
            IEnumerable<T> candidates,
            MarqueeRectangle rectangle,
            Func<T, MarqueePoint> pointSelector,
            ICollection<T> results)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (pointSelector == null)
            {
                throw new ArgumentNullException(nameof(pointSelector));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            foreach (T candidate in candidates)
            {
                if (rectangle.Contains(pointSelector(candidate)))
                {
                    results.Add(candidate);
                }
            }
        }
    }
}
