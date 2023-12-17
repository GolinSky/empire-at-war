using System;
using EmpireAtWar.Commands.Terrain;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Views.Terrain
{
    public class TerrainView:View<ITerrainModelObserver, ITerrainCommand>
    {
        protected override void OnInitialize()
        {
            
        }

        protected override void OnDispose()
        {
            
        }
    }
}