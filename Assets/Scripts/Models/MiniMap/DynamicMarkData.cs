using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public class DynamicMarkData:MarkData
    {
        protected Transform Transform { get; }
        public DynamicMarkData(Vector3 position, Sprite icon, Transform transform) : base(position, icon)
        {
            Transform = transform;
        }
    }
}