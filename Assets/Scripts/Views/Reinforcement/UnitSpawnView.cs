using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Views.Reinforcement
{
    public class UnitSpawnView : MonoBehaviour
    {
        [SerializeField] private float height;
        
        private List<Collider> triggeredCollider = new List<Collider>();
        private MeshRenderer[] meshRendererList;
        private List<Material> meshMaterials = new List<Material>();
        private Color canBeSpawnedColor;
        private Color blockedColor = Color.red;

        public bool CanSpawn => triggeredCollider.Count == 0;

        private void Start()
        {
            meshRendererList = GetComponentsInChildren<MeshRenderer>();
            for (var i = 0; i < meshRendererList.Length; i++)
            {
                meshMaterials.Add(meshRendererList[i].material);
            }
            canBeSpawnedColor = meshMaterials[0].color;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void UpdatePosition(Vector3 position)
        {
            position.y = height;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            for (var i = 0; i < meshMaterials.Count; i++)
            {
                meshMaterials[i].color = blockedColor;
            }
            
            if(!triggeredCollider.Contains(other))
                triggeredCollider.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if(triggeredCollider.Contains(other))
                triggeredCollider.Remove(other);
            
            if (triggeredCollider.Count == 0)
            {
                for (var i = 0; i < meshMaterials.Count; i++)
                {
                    meshMaterials[i].color = canBeSpawnedColor;
                }
            }
        }
    }
}