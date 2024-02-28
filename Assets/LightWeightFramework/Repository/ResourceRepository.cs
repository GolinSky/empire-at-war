using UnityEngine;

namespace LightWeightFramework.Components.Repository
{
    public class ResourceRepository : IRepository
    {
        public TSource Load<TSource>(string key)
            where TSource : Object
        {
            return Resources.Load<TSource>(key);
        }

        public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
        {
            return Resources.Load<TComponent>(key);
        }
    }
}