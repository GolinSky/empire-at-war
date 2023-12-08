using System.Collections.Generic;
using UnityEngine;
using WorkShop.LightWeightFramework.Factory;

namespace LightWeightFramework.Model
{
    public interface IModelHub
    {
        TModel GetModel<TModel>(string entityId) where TModel : Object, IModel;
        TModel GetModel<TModel>() where TModel : IModelObserver;
    }
    
    public class ModelHub:IModelHub
    {
        private readonly IFeatureFactory factory;
        private List<IModel> modelsList = new List<IModel>();

        public ModelHub(IFeatureFactory factory)
        {
            this.factory = factory;
        }
        public TModel GetModel<TModel>(string entityId)
            where TModel : Object, IModel
        {
            foreach (var model in modelsList)
            {
                if (model is TModel targetModel)
                {
                    return targetModel;
                }
            }


            var newModel = factory.CreateModel<TModel>(entityId);
            modelsList.Add(newModel);
            newModel.Init();
            return newModel;
        }

        public TModel GetModel<TModel>() where TModel :  IModelObserver
        {
            foreach (var model in modelsList)
            {
                if (model is TModel targetModel)
                {
                    return targetModel;
                }
            }

            return default;
        }
    }
}