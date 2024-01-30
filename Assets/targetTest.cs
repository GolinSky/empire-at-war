using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar
{
    public class targetTest : MonoBehaviour
    {
        public static targetTest Instance;

        private void Awake()
        {
            Instance = this;
        }

        public Vector3 Position => transform.position;
    }
}
