using System;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class NewBehaviourScript : MonoBehaviour
    {
        [Inject]
        private ShipView.ShipFactory ShipFactory;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
   
                ShipFactory.Create();
            }
        }
    }
}
