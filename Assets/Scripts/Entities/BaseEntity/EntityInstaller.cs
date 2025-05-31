using EmpireAtWar.Extentions;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Views.ViewImpl;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public class EntityInstaller: Installer
    {
        private readonly View _view;
        private readonly IUniqueIdGenerator _uniqueIdGenerator;

        public EntityInstaller(View view,  IUniqueIdGenerator uniqueIdGenerator)
        {
            _view = view;
            _uniqueIdGenerator = uniqueIdGenerator;
        }
        
        public override void InstallBindings()
        {
            Container.BindInterfacesNonLazyExt<Entity>();
            Container.BindInterfacesAndSelfTo<ViewEntity>().FromNewComponentOn(_view.gameObject).AsSingle().NonLazy();

            Container.BindEntity(_uniqueIdGenerator.GenerateUniqueId());
        }
    }
}