using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Mvc
{
    public abstract class Model : Data, IModel
    {
        [SerializeField] protected List<Model> models;

        public List<IModel> CurrentModels { get; } = new List<IModel>();

        protected virtual void Awake()
        {
            if (models == null)
            {
                return;
            }

            foreach (Model innerModel in models)
            {
                AddModel(innerModel);
            }
        }

        public virtual TModelObserver GetModelObserver<TModelObserver>()
            where TModelObserver : IModelObserver
        {
            if (this is TModelObserver modelObserver)
            {
                return modelObserver;
            }

            return GetModelInternal<TModelObserver>();
        }

        public virtual TModelObserver GetModel<TModelObserver>() where TModelObserver : IModel
        {
            return GetModelInternal<TModelObserver>();
        }

        protected virtual TModelObserver GetModelInternal<TModelObserver>()
        {
            foreach (IModel model in CurrentModels)
            {
                if (model is TModelObserver modelObserver)
                {
                    return modelObserver;
                }
            }

            return default;
        }

        protected virtual Model AddModel(Model model)
        {
            Model instancedModel = Instantiate(model);
            CurrentModels.Add(instancedModel);
            return instancedModel;
        }

        protected virtual void AddInnerModels(params InnerModel[] innerModels)
        {
            foreach (InnerModel innerModel in innerModels)
            {
                innerModel.Init();
                CurrentModels.Add(innerModel);
            }
        }
    }

    [Serializable]
    public abstract class InnerModel : PureModel, IModel
    {
        public virtual TModelObserver GetModelObserver<TModelObserver>()
            where TModelObserver : IModelObserver
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

        protected virtual void OnInit()
        {
        }
    }
}
