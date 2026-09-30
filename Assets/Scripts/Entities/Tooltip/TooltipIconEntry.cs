using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [Serializable]
    public sealed class TooltipIconEntry
    {
        [SerializeField] private string key;
        [SerializeField] private Sprite sprite;
        public string Key => key;
        public Sprite Sprite => sprite;
    }
}
