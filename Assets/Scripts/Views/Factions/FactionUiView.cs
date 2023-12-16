using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUiView:View<IFactionModelObserver>
    {
        [SerializeField] private Canvas controlCanvas;
        
        protected override void OnInitialize()
        {
            controlCanvas.enabled = false;
        }

        protected override void OnDispose()
        {
            
        }
    }
}