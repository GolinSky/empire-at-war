using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [Serializable]
    public sealed class TooltipIconEntry
    {
        [SerializeField] private Sprite sprite;

        [SerializeField] private string key;

        public string Key => key;
        public Sprite Sprite => sprite;
    }
}
