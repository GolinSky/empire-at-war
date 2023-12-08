using UnityEngine;
using UnityEngine.AddressableAssets;
using WorkShop.LightWeightFramework.Repository;

namespace EmpireAtWar.Repository
{
    public class AddressableRepository:IRepository
    {
        public TSource Load<TSource>(string key) where TSource : Object
        {
            return Addressables.LoadAssetAsync<TSource>(key).WaitForCompletion();
        }
    }
}