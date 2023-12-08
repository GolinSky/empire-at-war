using UnityEngine;

namespace WorkShop.LightWeightFramework.Repository
{
    public class ResourceRepository:IRepository
    {
        public TSource Load<TSource>(string key)
            where TSource : Object
        {
            return Resources.Load<TSource>(key);
        }
    }
}