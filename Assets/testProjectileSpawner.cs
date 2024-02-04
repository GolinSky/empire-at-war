using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar
{
    public class testProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> particleSystem;
        
        private targetTest targetTest;
        private void Start()
        {
            targetTest = EmpireAtWar.targetTest.Instance;
        }

        private void Update()
        {
            foreach (ParticleSystem system in particleSystem)
            {
                float distance = Vector3.Distance(targetTest.Position, system.transform.position);
                var main = system.main;
                main.startLifetime = distance / main.startSpeedMultiplier;
                
                Vector3 eulerAngles = Quaternion.LookRotation (transform.position-targetTest.Position).eulerAngles;
                //eulerAngles.x = system.transform.position.x > 1 ? 90 : -90;
               // system.transform.rotation = Quaternion.Euler(eulerAngles);
               // system.transform.rotation =  Quaternion.LookRotation (system.transform.position -targetTest.Position , Vector3.forward);
                
                // Determine which direction to rotate towards

                system.transform.LookAt(targetTest.transform);
                
            }
        }
    }
}
