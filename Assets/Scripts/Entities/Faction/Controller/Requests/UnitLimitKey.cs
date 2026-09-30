using System;

namespace EmpireAtWar.Controllers.Factions
{
    public readonly struct UnitLimitKey : IEquatable<UnitLimitKey>
    {
        public Type RequestType { get; }
        public string Id { get; }

        public UnitLimitKey(Type requestType, string id)
        {
            while (requestType.BaseType != null &&
                   requestType.BaseType != typeof(UnitRequest) &&
                   !(requestType.BaseType.IsGenericType &&
                     requestType.BaseType.GetGenericTypeDefinition() == typeof(UnitRequest<>)) &&
                   typeof(UnitRequest).IsAssignableFrom(requestType.BaseType))
                requestType = requestType.BaseType;
            RequestType = requestType;
            Id = id;
        }

        public static UnitLimitKey From(UnitRequest request) => new UnitLimitKey(request.GetType(), request.Id);
        public static UnitLimitKey For<TRequest>(string id) => new UnitLimitKey(typeof(TRequest), id);

        public bool Equals(UnitLimitKey other) => RequestType == other.RequestType && Id == other.Id;
        public override bool Equals(object obj) => obj is UnitLimitKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RequestType, Id);
    }
}
