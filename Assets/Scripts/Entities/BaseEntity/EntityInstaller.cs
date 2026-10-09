using EmpireAtWar.Extensions;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Presenters.MiniMap;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public class EntityInstaller: Installer
    {
        private readonly IUniqueIdGenerator _uniqueIdGenerator;

        private readonly Component _entity;

        public EntityInstaller(IUniqueIdGenerator uniqueIdGenerator, Component entity)
        {
            _entity = entity;
            _uniqueIdGenerator = uniqueIdGenerator;
        }

        public override void InstallBindings()
        {
            Container.BindInterfacesNonLazyExt<Entity>();
            Container.BindInterfacesExt<EntityTransformFacade>();
            Container.BindInterfacesAndSelfTo<ViewEntity>().FromNewComponentOn(_entity.gameObject).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MiniMapUnitMarkerPresenter>().AsSingle().NonLazy();

            Container.BindEntityExt(_uniqueIdGenerator.GenerateUniqueId());
        }
    }
}
