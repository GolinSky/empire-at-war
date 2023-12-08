using System.Collections.Generic;
using LightWeightFramework.Controller;

namespace WorkShop.LightWeightFramework.Game
{
    public interface IGameLevelData
    {
        int SceneIndex { get; }

        IEnumerable<IController> GetEntities(IGameObserver gameObserver);
        //data of entities on current lvl - serialized

    }
}