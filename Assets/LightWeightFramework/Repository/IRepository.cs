using UnityEngine;

namespace WorkShop.LightWeightFramework.Repository
{
    public interface IRepository
    {
        TSource Load<TSource>(string key) where TSource : Object;
    }
}