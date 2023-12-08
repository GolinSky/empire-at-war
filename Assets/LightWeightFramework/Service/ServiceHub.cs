using System.Collections.Generic;

namespace WorkShop.LightWeightFramework.Service
{
    public class ServiceHub : IServiceHub
    {
        protected readonly IEnumerable<IService> services;

        public ServiceHub(IEnumerable<IService> services)
        {
            this.services = services;
        }
        public TService Get<TService>() where TService : IService
        {
            foreach (var service in services)
            {
                if (service is TService entity)
                {
                    return entity;
                }
            }
        
            return default;
        }

     
    }
}