using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Service;

namespace WorkShop.LightWeightFramework.Game
{
    public interface IGameContext
    {
        IService[] Services { get; }
        
    }
  
}