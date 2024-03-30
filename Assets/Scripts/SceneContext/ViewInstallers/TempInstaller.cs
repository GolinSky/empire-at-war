using Zenject;

namespace EmpireAtWar.SceneContext.ViewInstallers
{
    public class TempInstaller:MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<MonoComponentInstaller>(new object[]{transform});
        }
    }
}