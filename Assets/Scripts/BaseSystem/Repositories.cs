using UnityEngine;

namespace EmpireAtWar.Mvc
{
    public interface IRepository
    {
        TSource Load<TSource>(string key) where TSource : Object;
        TComponent LoadComponent<TComponent>(string key) where TComponent : UnityEngine.Component;
    }

    public class ResourceRepository : IRepository
    {
        public TSource Load<TSource>(string key) where TSource : Object
        {
            return Resources.Load<TSource>(key);
        }

        public TComponent LoadComponent<TComponent>(string key) where TComponent : UnityEngine.Component
        {
            return Resources.Load<TComponent>(key);
        }
    }
}
