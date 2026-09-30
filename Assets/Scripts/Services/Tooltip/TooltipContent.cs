using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipContent : IEquatable<TooltipContent>
    {
        public TooltipContent(string title, string description = "", string iconKey = "",
            string subtitle = "", string shortcut = "", IEnumerable<TooltipStat> stats = null,
            IEnumerable<TooltipIcon> strongAgainst = null, IEnumerable<TooltipIcon> weakAgainst = null,
            IEnumerable<TooltipRequirement> requirements = null, string status = "")
        {
            Title = title;
            Description = description;
            IconKey = iconKey;
            Subtitle = subtitle;
            Shortcut = shortcut;
            Stats = Array.AsReadOnly((stats ?? Array.Empty<TooltipStat>()).ToArray());
            StrongAgainst = Array.AsReadOnly((strongAgainst ?? Array.Empty<TooltipIcon>()).ToArray());
            WeakAgainst = Array.AsReadOnly((weakAgainst ?? Array.Empty<TooltipIcon>()).ToArray());
            Requirements = Array.AsReadOnly((requirements ?? Array.Empty<TooltipRequirement>()).ToArray());
            Status = status;
        }

        public string Title { get; }
        public string Description { get; }
        public string IconKey { get; }
        public string Subtitle { get; }
        public string Shortcut { get; }
        public IReadOnlyList<TooltipStat> Stats { get; }
        public IReadOnlyList<TooltipIcon> StrongAgainst { get; }
        public IReadOnlyList<TooltipIcon> WeakAgainst { get; }
        public IReadOnlyList<TooltipRequirement> Requirements { get; }
        public string Status { get; }

        public bool Equals(TooltipContent other) => other != null &&
            Title == other.Title && Description == other.Description && IconKey == other.IconKey &&
            Subtitle == other.Subtitle && Shortcut == other.Shortcut && Status == other.Status &&
            Stats.SequenceEqual(other.Stats) && StrongAgainst.SequenceEqual(other.StrongAgainst) &&
            WeakAgainst.SequenceEqual(other.WeakAgainst) && Requirements.SequenceEqual(other.Requirements);
        public override bool Equals(object obj) => obj is TooltipContent other && Equals(other);
        public override int GetHashCode() => Title.GetHashCode();
    }
}
