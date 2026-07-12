using EmpireAtWar.Extentions;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public class EntityInstaller: Installer
    {
        private readonly Component _entity;
        private readonly IUniqueIdGenerator _uniqueIdGenerator;

        public EntityInstaller(Component entity, IUniqueIdGenerator uniqueIdGenerator)
        {
            _entity = entity;
            _uniqueIdGenerator = uniqueIdGenerator;
        }
        
        public override void InstallBindings()
        {
            Container.BindInterfacesNonLazyExt<Entity>();
            Container.BindInterfacesAndSelfTo<ViewEntity>().FromNewComponentOn(_entity.gameObject).AsSingle().NonLazy();

            Container.BindEntityExt(_uniqueIdGenerator.GenerateUniqueId());
        }
    }
}
