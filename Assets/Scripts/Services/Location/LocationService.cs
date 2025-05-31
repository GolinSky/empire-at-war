using System;
using LightWeightFramework;
using LightWeightFramework.Components.Service;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.Location
{

    public interface ILocationService : IService
    {
        Transform GetLocation(LocationType locationType);
    }
    
    //todo: create monobehaviour service 
    public class LocationService: MonoBehaviour, ILocationService
    {
        [SerializeField] private DictionaryWrapper<LocationType, Transform> locations;
        
        string IEntity.Id => GetType().Name;
        
        public Transform GetLocation(LocationType locationType)
        {
            locations.Dictionary.TryGetValue(locationType, out var location);
            Assert.IsNotNull(location);
            return location;
        }
    }

    [Serializable]
    public enum LocationType
    {
        UiCanvas = 0,
        
    }
}