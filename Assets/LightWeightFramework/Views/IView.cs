using LightWeightFramework.Model;

namespace WorkShop.LightWeightFramework
{
    
    //add command
    public interface IView
    {
        void Init(IModelObserver model);
        void Release();
    }
}