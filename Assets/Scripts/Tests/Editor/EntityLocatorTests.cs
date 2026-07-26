using System.Reflection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EntityLocatorTests
    {
        [Test]
        public void TryGetEntity_ResolvesViewEntityFromChildCollider()
        {
            const long ID = 42;
            GameObject root = new GameObject("Entity");
            ViewEntity viewEntity = root.AddComponent<ViewEntity>();
            SetViewEntityId(viewEntity, ID);
            GameObject child = new GameObject("ChildCollider");
            child.transform.SetParent(root.transform);
            BoxCollider collider = child.AddComponent<BoxCollider>();
            EntityLocator locator = new EntityLocator();
            FakeEntity expected = new FakeEntity(ID);
            locator.AddEntity(expected);

            bool found = locator.TryGetEntity(collider, out IEntity actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
            Object.DestroyImmediate(root);
        }

        private static void SetViewEntityId(ViewEntity viewEntity, long id)
        {
            FieldInfo field = typeof(ViewEntity).GetField(
                "<Id>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(viewEntity, id);
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id)
            {
                Id = id;
            }

            public long Id { get; }
            public EmpireAtWar.Mvc.IModelObserver Model => null;
            public IHealthModelObserver HealthModel => null;
            public PlayerType PlayerType => PlayerType.Player;

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                entityCommand = default;
                return false;
            }
        }
    }
}
