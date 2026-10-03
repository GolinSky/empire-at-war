using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public class MarkData:IMarkData
    {
        public virtual Vector3 Position { get; }
        public Sprite Icon { get; }

        public MarkData(Sprite icon, Vector3 position)
        {
            Position = position;
            Icon = icon;
        }
    }
}