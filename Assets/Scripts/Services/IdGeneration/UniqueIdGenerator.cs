using System;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.IdGeneration
{
    public interface IUniqueIdGenerator: IService
    {
        long GenerateUniqueId();
    }
    
    public class UniqueIdGenerator: Service, IUniqueIdGenerator
    {
        public long GenerateUniqueId()
        {
            return BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        }
    }
}