using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [CreateAssetMenu(fileName = nameof(TooltipIconData), menuName = "Data/Tooltip/Icons")]
    public sealed class TooltipIconData : Data
    {
        [SerializeField] private TooltipIconEntry[] icons;
        private Dictionary<string, Sprite> _sprites;
        public Sprite Resolve(string key)
        {
            if (_sprites == null)
            {
                _sprites = new Dictionary<string, Sprite>();
                foreach (TooltipIconEntry icon in icons) _sprites.Add(icon.Key, icon.Sprite);
            }
            return _sprites[key];
        }
    }
}
