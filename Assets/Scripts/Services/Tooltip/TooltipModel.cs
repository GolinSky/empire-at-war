using System;

namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipModel : ITooltipModelObserver
    {
        public event Action Shown;
        public event Action ContentChanged;
        public event Action AnchorChanged;
        public event Action Hidden;
        public bool IsVisible { get; private set; }
        public TooltipContent Content { get; private set; }
        public TooltipAnchor Anchor { get; private set; }

        public void Show(TooltipContent content, TooltipAnchor anchor)
        {
            Content = content;
            Anchor = anchor;
            IsVisible = true;
            Shown?.Invoke();
        }

        public void Refresh(TooltipContent content)
        {
            if (Content.Equals(content)) return;
            Content = content;
            ContentChanged?.Invoke();
        }

        public void SetAnchor(TooltipAnchor anchor)
        {
            if (Anchor.Equals(anchor)) return;
            Anchor = anchor;
            if (IsVisible) AnchorChanged?.Invoke();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Hidden?.Invoke();
        }
    }
}
