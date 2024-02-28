using System;

namespace LightWeightFramework.Model
{
    [Serializable]
    public abstract class InnerModel:IModel
    {
        public virtual TModelObserver GetModelObserver<TModelObserver>() where TModelObserver : IModelObserver
        {
            return default;
        }

        public virtual TModelObserver GetModel<TModelObserver>() where TModelObserver : IModel
        {
            return default;
        }

        public void Init()
        {
            OnInit();
        }

        protected virtual void OnInit() {}
    }
}