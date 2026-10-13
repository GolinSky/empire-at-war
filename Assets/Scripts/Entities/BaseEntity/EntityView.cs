using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    public abstract class EntityView : MonoBehaviour, IController
    {
        [SerializeField] private EntityBindings bindings;

        public EntityBindings Bindings => bindings;
        public abstract string Id { get; }
    }
}
