using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public class MarkData:IMarkData
    {
        public virtual Vector3 Position { get; }
        public Sprite Icon { get; }
        
        public MarkData(Vector3 position, Sprite icon)
        {
            Position = position;
            Icon = icon;
        }
    }
}