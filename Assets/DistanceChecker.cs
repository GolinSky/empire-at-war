using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar
{
    public class DistanceChecker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"distance:{Vector3.Distance(target.position, transform.position)}");
            }
        }
    }
}
