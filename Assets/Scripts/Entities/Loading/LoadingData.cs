using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Loading
{
    public interface ILoadingModelObserver : IModelObserver
    {
        
    }
    
    [CreateAssetMenu(fileName = nameof(LoadingData), menuName = "Data/LoadingData")]
    public class LoadingData:Data, IModel, ILoadingModelObserver
    {
        
    }
}