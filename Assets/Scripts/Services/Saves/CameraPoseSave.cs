using System;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class CameraPoseSave
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private float zoomTargetHeight;

        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public float ZoomTargetHeight => zoomTargetHeight;

        public static CameraPoseSave Create(Vector3 position, Quaternion rotation, float zoomTargetHeight) =>
            new CameraPoseSave { position = position, rotation = rotation, zoomTargetHeight = zoomTargetHeight };
    }
}
