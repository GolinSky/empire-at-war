using EmpireAtWar.Models.Planet;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.Planet
{
    public class PlanetView:View<IPlanetModelObserver>, ITickable
    {
        [SerializeField] protected Transform planetTransform;
        [SerializeField] private Transform cloudTransform;
        
        protected override void OnInitialize()
        {
            
        }

        protected override void OnDispose()
        {
            
        }

        public void Tick()
        {
            planetTransform.eulerAngles += Model.PlanetRotation;
            cloudTransform.eulerAngles += Model.CloudRotation;
        }
    }
}