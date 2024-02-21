using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.ViewComponents;

namespace WorkShop.LightWeightFramework
{
    public interface IView
    {
        Transform Transform { get; }
        ViewComponent[] ViewComponents { get; }
        void Init(IModelObserver model);
        void Release();
    }
}