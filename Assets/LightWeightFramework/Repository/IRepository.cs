using UnityEngine;

namespace LightWeightFramework.Components.Repository
{
    public interface IRepository
    {
        TSource Load<TSource>(string key) where TSource : Object;
        TComponent LoadComponent<TComponent>(string key) where TComponent : Component;

    }
}