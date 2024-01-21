using LightWeightFramework.Model;

namespace WorkShop.LightWeightFramework
{
    public interface IView
    {
        void Init(IModelObserver model);
        void Release();
    }
}