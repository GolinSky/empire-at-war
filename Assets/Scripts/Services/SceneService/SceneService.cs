using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneService : IService
    {
        
    }
    
    public class SceneService:Service, ISceneService
    {
        protected override void OnInit(IGameObserver gameObserver)
        {
            
        }

        protected override void Release()
        {
            
        }
    }
}