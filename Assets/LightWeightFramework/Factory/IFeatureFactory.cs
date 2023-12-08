using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;

namespace WorkShop.LightWeightFramework.Factory
{
    public interface IFeatureFactory
    {
        TController CreateController<TController, TModel>(TModel model)
            where TController : Controller<TModel>
            where TModel : IModel;

  
        View CreateView(string entityId);

        TModel CreateModel<TModel>(string entityId) where TModel :Object, IModel;
     
    }
}