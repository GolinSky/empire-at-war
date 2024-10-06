using UnityEngine;
using UnityEngine.AddressableAssets;
using LightWeightFramework.Components.Repository;

namespace EmpireAtWar.Repository
{
    public class AddressableRepository:IRepository
    {
        public TSource Load<TSource>(string key) where TSource : Object
        {
            return Addressables.LoadAssetAsync<TSource>(key).WaitForCompletion();
        }

        public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
        {
            return Addressables.LoadAssetAsync<GameObject>(key).WaitForCompletion().GetComponent<TComponent>();
        }
    }
}