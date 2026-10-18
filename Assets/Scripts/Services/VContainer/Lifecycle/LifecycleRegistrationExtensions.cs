using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Lifecycle
{
    public static class LifecycleRegistrationExtensions
    {
        public static RegistrationBuilder RegisterLifecycle(this IContainerBuilder builder)
        {
            builder.RegisterEntryPointExceptionHandler(exception => { throw exception; });
            builder.Register<LifecycleTickGate>(Lifetime.Singleton);
            return builder.RegisterEntryPoint<OrderedLifecycleCoordinator>().AsSelf();
        }
    }
}
