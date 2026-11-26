using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Units.Matchups
{
    /// <summary>How tooltips name and draw a set of classes or roles ("Capital ships", "Artillery ships").</summary>
    [Serializable]
    public sealed class MatchupGroup
    {
        [SerializeField] private string label;
        [SerializeField] private MatchupSide members;
        [SerializeField] private MatchupIcon[] icons = Array.Empty<MatchupIcon>();
        [Tooltip("Shown for every faction; for groups no faction owns, such as stations.")]
        [SerializeField] private string neutralIconKey;

        public string Label => label;
        public MatchupSide Members => members;
        public MatchupIcon[] Icons => icons;
        public string NeutralIconKey => neutralIconKey;

        public MatchupGroup(string label, MatchupSide members, MatchupIcon[] icons, string neutralIconKey = "")
        {
            this.neutralIconKey = neutralIconKey;
            this.label = label;
            this.members = members;
            this.icons = icons;
        }
    }
}
