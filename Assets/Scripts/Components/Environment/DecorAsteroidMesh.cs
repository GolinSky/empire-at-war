using System;
using EmpireAtWar.Entities.Map;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    [Serializable]
    public sealed class DecorAsteroidMesh
    {
        [field: SerializeField] public AsteroidSize Size { get; private set; }
        [field: SerializeField] public Mesh Mesh { get; private set; }
        [field: SerializeField] public Mesh LowDetailMesh { get; private set; }
        [field: SerializeField] public Material[] Materials { get; private set; }
        [field: SerializeField] public Matrix4x4 LocalTransform { get; private set; } = Matrix4x4.identity;

        public DecorAsteroidMesh(AsteroidSize size, Mesh mesh, Material[] materials, Matrix4x4 localTransform,
            Mesh lowDetailMesh = null)
        {
            Size = size;
            Mesh = mesh;
            LowDetailMesh = lowDetailMesh;
            Materials = materials;
            LocalTransform = localTransform;
        }
    }
}
