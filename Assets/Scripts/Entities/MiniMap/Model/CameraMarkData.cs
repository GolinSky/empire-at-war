using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public class CameraMarkData : DynamicMarkData
    {
        private Vector3 _position;
        
        public override Vector3 Position
        {
            get
            {
                _position = Transform.position;
                
                double b = _position.y * Mathf.Sin(Transform.rotation.eulerAngles.x); 
                _position.z -= (float)b;
                return _position;
            }
        }

        public CameraMarkData(Vector3 position, Sprite icon, Transform transform) : base(position, icon, transform)
        {
        }
    }
}