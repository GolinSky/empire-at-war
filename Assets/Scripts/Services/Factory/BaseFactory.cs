using EmpireAtWar.Mvc;
using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Services.Factory
{
    public abstract class BaseFactory
    {
        private readonly LifetimeScope _scope;
        private readonly EntityScopeOwnership _ownership;
        protected IAssetService Assets { get; }

        protected BaseFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership)
        {
            Assets = assets;
            _scope = scope;
            _ownership = ownership;
        }

        protected TEntity Spawn<TEntity>(TEntity prefab, Vector3 position, Quaternion rotation,
            Action<IContainerBuilder, TEntity> install) where TEntity : EntityView
        {
            GameObject staging = new GameObject(typeof(TEntity).Name + " Spawn");
            staging.SetActive(false);
            EntityLifetimeScope child = null;
            try
            {
                TEntity entity = UnityEngine.Object.Instantiate(prefab, staging.transform, false);
                entity.gameObject.SetActive(false);
                entity.transform.SetPositionAndRotation(position, rotation);
                child = _scope.CreateChild<EntityLifetimeScope>(builder =>
                {
                    entity.transform.SetParent(child.transform, true);
                    install(builder, entity);
                    builder.RegisterLifecycle();
                }, typeof(TEntity).Name + " Scope");
                child.SetOwnership(_ownership);
                _ownership.Register(child);
                child.Build();
                entity.gameObject.SetActive(true);
                return entity;
            }
            catch (Exception spawnException)
            {
                try
                {
                    if (child != null)
                    {
                        try
                        {
                            child.DisposeCore();
                        }
                        finally
                        {
                            _ownership.Unregister(child);
                            DestroyObject(child.gameObject);
                        }
                    }
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(spawnException, cleanupException);
                }
                throw;
            }
            finally
            {
                DestroyObject(staging);
            }
        }

        private static void DestroyObject(GameObject instance)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(instance);
            else UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
