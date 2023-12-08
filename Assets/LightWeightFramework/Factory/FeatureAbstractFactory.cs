using System;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Repository;
using Object = UnityEngine.Object;

namespace WorkShop.LightWeightFramework.Factory
{
    public sealed class FeatureAbstractFactory:IFeatureFactory
    {
        private readonly IRepository repository;

        public FeatureAbstractFactory(IRepository repository)
        {
            this.repository = repository;
        }
        
        public TController CreateController<TController, TModel>(TModel model)
            where TController : Controller<TModel>
            where TModel : IModel
        {
            return (TController)Activator.CreateInstance(typeof(TController), model);
        }

        public View CreateView(string entityId)
        {
            var obj = repository.Load<GameObject>(entityId);
            return Object.Instantiate(obj).GetComponent<View>();
        }

        public TModel CreateModel<TModel>(string entityId) where TModel : Object, IModel
        {
            return Object.Instantiate(repository.Load<TModel>(entityId));
        }
    }
}