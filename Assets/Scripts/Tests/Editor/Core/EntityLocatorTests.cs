using System.Reflection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class EntityLocatorTests
    {
        [Test]
        public void Entities_EnumeratesLiveContentsWithoutAllocatingThroughLocatorInterface()
        {
            IEntityLocator locator = new EntityLocator();
            var first = new FakeEntity(1);
            var second = new FakeEntity(2);
            locator.AddEntity(first);
            locator.AddEntity(second);
            foreach (IEntity entity in locator.Entities) _ = entity.Id;

            long total = 0;
            GcAllocationAssert.DoesNotAllocate(() =>
            {
                total = 0;
                for (int iteration = 0; iteration < 100; iteration++)
                    foreach (IEntity entity in locator.Entities) total += entity.Id;
            });

            Assert.That(total, Is.EqualTo(300));
            locator.RemoveEntity(first);
            Assert.That(locator.Entities, Is.EquivalentTo(new[] { second }));
        }

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
            public long Id { get; }
            public IHealthModelObserver HealthModel => null;
            public PlayerId Owner => TestPlayers.Human;

            public FakeEntity(long id)
            {
                Id = id;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            {
                entityCommand = default;
                return false;
            }
        }
    }
}
