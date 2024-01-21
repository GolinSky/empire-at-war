using System.Collections.Generic;
using UnityEngine;

namespace LightWeightFramework.Model
{
    public abstract class Model:ScriptableObject, IModel
    {
        private List<IModel> CurrentModels { get; } = new List<IModel>();
        
        public TModelObserver GetModelObserver<TModelObserver>() where TModelObserver : IModelObserver
        {
            if (this is TModelObserver modelObserver) return modelObserver;//fix this - create inner model
            
            return GetModelInternal<TModelObserver>();
        }

        public TModelObserver GetModel<TModelObserver>() where TModelObserver : IModel
        {
            return GetModelInternal<TModelObserver>();
        }

        private TModelObserver GetModelInternal<TModelObserver>() 
        {
            foreach (var model in CurrentModels)
            {
                if (model is TModelObserver modelObserver)
                {
                    return modelObserver;
                }
            }

            return default;
        }

        protected TModel AddInnerModel<TModel>(TModel model) where TModel : Object, IModel
        {
            var instancedModel = Instantiate(model);
            CurrentModels.Add(instancedModel);
            return instancedModel;
        }
    }
}