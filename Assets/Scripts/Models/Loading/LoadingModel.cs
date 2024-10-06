using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Loading
{
    public interface ILoadingModelObserver : IModelObserver
    {
        
    }
    
    [CreateAssetMenu(fileName = "LoadingModel", menuName = "Model/LoadingModel")]
    public class LoadingModel:Model, ILoadingModelObserver
    {
        
    }
}