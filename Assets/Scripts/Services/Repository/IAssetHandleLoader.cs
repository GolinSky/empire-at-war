using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EmpireAtWar.Repository
{
    public interface IAssetHandleLoader
    {
        AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object;
    }
}
