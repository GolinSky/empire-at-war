using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public class CameraMarkData : DynamicMarkData
    {
        private Vector3 position;
        
        public override Vector3 Position
        {
            get
            {
                position = Transform.position;
                
                double b = position.y * Mathf.Sin(Transform.rotation.eulerAngles.x); 
                position.z -= (float)b;
                return position;
            }
        }

        public CameraMarkData(Vector3 position, Sprite icon, Transform transform) : base(position, icon, transform)
        {
        }
    }
}