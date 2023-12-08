using System;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.SceneContext
{
    public class MyCustomGameContext : IGameContext
    {
        public IService[] Services => Array.Empty<IService>();
    }
}