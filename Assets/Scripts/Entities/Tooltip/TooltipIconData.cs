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

        private Dictionary<string, Sprite> Sprites
        {
            get
            {
                if (_sprites == null)
                {
                    _sprites = new Dictionary<string, Sprite>();
                    foreach (TooltipIconEntry icon in icons) _sprites.Add(icon.Key, icon.Sprite);
                }
                return _sprites;
            }
        }

        public string Register(Sprite sprite)
        {
            // Runtime keys stay in tooltip content and are never serialized.
            string key = $"runtime:{sprite.GetEntityId()}";
            Sprites[key] = sprite;
            return key;
        }

        public Sprite Resolve(string key) => Sprites[key];
    }
}
