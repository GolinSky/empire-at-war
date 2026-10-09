using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using Zenject.Internal;

namespace EmpireAtWar.Extensions
{
    public static class InstallerExtensions
    {
        public static TComponent InstantiatePrefabForInstall<TComponent>(
            this DiContainer container, TComponent prefab, Transform parent, GameObjectContext context)
            where TComponent : Component
        {
            var inactiveParent = new GameObject("InstallerPrefabParent");
            inactiveParent.SetActive(false);

            TComponent component;
            try
            {
                component = UnityEngine.Object.Instantiate(prefab, inactiveParent.transform, false);
                component.gameObject.SetActive(false);
                component.transform.SetParent(parent, false);
            }
            finally
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(inactiveParent);
                else
                    UnityEngine.Object.DestroyImmediate(inactiveParent);
            }

            ZenUtilInternal.AddStateMachineBehaviourAutoInjectersUnderGameObject(component.gameObject);
            var behaviours = new List<MonoBehaviour>();
            ZenUtilInternal.GetInjectableMonoBehavioursUnderGameObject(component.gameObject, behaviours);
            foreach (MonoBehaviour behaviour in behaviours)
                container.QueueForInject(behaviour);

            if (prefab.gameObject.activeSelf && !container.IsValidating)
                context.PostResolve += Activate;

            return component;

            void Activate()
            {
                context.PostResolve -= Activate;
                component.gameObject.SetActive(true);
            }
        }

        //todo: rename it 
        public static DiContainer BindInterfacesExt<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle();
            return container;
        }

        public static DiContainer BindInterfacesExt<TEntity>(this DiContainer container, object id)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .WithConcreteId(id);
            
            return container;
        }

        public static DiContainer BindInterfacesNonLazyExt<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .NonLazy();
            return container;
        }

        public static DiContainer BindModel<TModel>(
            this DiContainer container,
            IAssetService assetService,
            string prefix = null,
            string postfix = null)
         where TModel: Data
        {
            ModelDependencyBuilder
                .ConstructBuilder(container)
                .AppendToPath(prefix, postfix)
                .BindFromNewScriptable<TModel>(assetService);
            return container;
        }

        public static DiContainer BindScriptableObject<T>(
            this DiContainer container,
            IAssetService assetService,
            string path = null)
            where T: ScriptableObject
        {
            container
                .BindInterfacesAndSelfTo<T>()
                .FromNewScriptableObject(assetService.Load<T>(path ?? ConstructName<T>()))
                .AsSingle();
            return container;
        }

        public static ConcreteIdArgConditionCopyNonLazyBinder BindEntityExt<TEntity>(this DiContainer container, TEntity entity)
        {
            var binder =  container
                .BindInstance(@entity)
                .AsSingle();
            return binder;
        }

        private static string ConstructName<T>()
        {
            return typeof(T).Name;
        }
    }
}
