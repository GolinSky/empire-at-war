using EmpireAtWar.Extentions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MonoComponentInstaller:Installer
    {
        private readonly Transform _transform;

        public MonoComponentInstaller(Transform transform)
        {
            _transform = transform;
        }
        public override void InstallBindings()
        {
            Container
                .Bind<Transform>()
                .WithId(EntityBindType.ViewTransform)
                .FromMethod(() => _transform)
                .NonLazy();

        }
    }
}