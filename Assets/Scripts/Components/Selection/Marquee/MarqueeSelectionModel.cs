using System;

namespace EmpireAtWar.Components.Selection.Marquee
{
    public readonly struct MarqueePoint
    {
        public MarqueePoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
    }

    public readonly struct MarqueeRectangle
    {
        public MarqueeRectangle(MarqueePoint first, MarqueePoint second)
        {
            MinX = Math.Min(first.X, second.X);
            MinY = Math.Min(first.Y, second.Y);
            MaxX = Math.Max(first.X, second.X);
            MaxY = Math.Max(first.Y, second.Y);
        }

        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;

        public bool Contains(MarqueePoint point)
        {
            return point.X >= MinX && point.X <= MaxX &&
                   point.Y >= MinY && point.Y <= MaxY;
        }
    }

    public sealed class MarqueeSelectionModel
    {
        private MarqueePoint _start;

        public bool IsActive { get; private set; }
        public MarqueeRectangle Rectangle { get; private set; }

        public void Begin(MarqueePoint start)
        {
            _start = start;
            Rectangle = new MarqueeRectangle(start, start);
            IsActive = true;
        }

        public void Update(MarqueePoint current)
        {
            if (!IsActive)
            {
                return;
            }

            Rectangle = new MarqueeRectangle(_start, current);
        }

        public MarqueeRectangle Complete(MarqueePoint end)
        {
            Update(end);
            IsActive = false;
            return Rectangle;
        }

        public void Cancel()
        {
            IsActive = false;
        }
    }
}
