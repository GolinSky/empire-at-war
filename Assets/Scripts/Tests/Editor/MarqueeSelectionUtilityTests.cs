using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using NUnit.Framework;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Tests.Selection
{
    public sealed class MarqueeSelectionUtilityTests
    {
        [Test]
        public void Complete_NormalizesRectangleDraggedInReverseDirection()
        {
            MarqueeSelectionModel model = new MarqueeSelectionModel();

            model.Begin(new MarqueePoint(20f, 30f));
            MarqueeRectangle rectangle = model.Complete(new MarqueePoint(5f, 10f));

            Assert.That(rectangle.MinX, Is.EqualTo(5f));
            Assert.That(rectangle.MinY, Is.EqualTo(10f));
            Assert.That(rectangle.MaxX, Is.EqualTo(20f));
            Assert.That(rectangle.MaxY, Is.EqualTo(30f));
        }

        [Test]
        public void CollectInside_IncludesBoundaryAndExcludesOutsidePoints()
        {
            MarqueeRectangle rectangle = new MarqueeRectangle(
                new MarqueePoint(5f, 10f),
                new MarqueePoint(20f, 30f));
            List<MarqueePoint> points = new List<MarqueePoint>
            {
                new MarqueePoint(5f, 10f),
                new MarqueePoint(12f, 15f),
                new MarqueePoint(21f, 15f)
            };
            List<MarqueePoint> selected = new List<MarqueePoint>();

            MarqueeSelectionUtility.CollectInside(points, rectangle, point => point, selected);

            Assert.That(selected, Has.Count.EqualTo(2));
            Assert.That(selected, Does.Contain(points[0]));
            Assert.That(selected, Does.Contain(points[1]));
        }

        [Test]
        public void Replace_UpdatesTheWholeGroupWithoutRetogglingRetainedEntities()
        {
            FakeSelectionCommand firstCommand = new FakeSelectionCommand(SelectionType.Ship);
            FakeSelectionCommand secondCommand = new FakeSelectionCommand(SelectionType.Ship);
            FakeEntity first = new FakeEntity(1, firstCommand);
            FakeEntity second = new FakeEntity(2, secondCommand);
            SelectionContext context = new SelectionContext(PlayerType.Player);

            context.Replace(new[]
            {
                new SelectionEntry(first, firstCommand),
                new SelectionEntry(second, secondCommand)
            });
            context.Replace(new[] { new SelectionEntry(second, secondCommand) });

            Assert.That(context.Count, Is.EqualTo(1));
            Assert.That(context.Entity, Is.SameAs(second));
            Assert.That(firstCommand.IsSelected, Is.False);
            Assert.That(secondCommand.IsSelected, Is.True);
            Assert.That(firstCommand.ChangeCount, Is.EqualTo(2));
            Assert.That(secondCommand.ChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void Replace_UsesNoneAsTheCommonTypeForMixedSelections()
        {
            FakeSelectionCommand shipCommand = new FakeSelectionCommand(SelectionType.Ship);
            FakeSelectionCommand baseCommand = new FakeSelectionCommand(SelectionType.Base);
            SelectionContext context = new SelectionContext(PlayerType.Player);

            context.Replace(new[]
            {
                new SelectionEntry(new FakeEntity(1, shipCommand), shipCommand),
                new SelectionEntry(new FakeEntity(2, baseCommand), baseCommand)
            });

            Assert.That(context.SelectionType, Is.EqualTo(SelectionType.None));
        }

        private sealed class FakeSelectionCommand : IEntitySelectionCommand
        {
            public FakeSelectionCommand(SelectionType selectionType)
            {
                SelectionType = selectionType;
            }

            public SelectionType SelectionType { get; set; }
            public bool IsSelected { get; private set; }
            public int ChangeCount { get; private set; }

            public void Select(bool isSelected)
            {
                IsSelected = isSelected;
                ChangeCount++;
            }
        }

        private sealed class FakeEntity : GameEntity
        {
            private readonly IEntitySelectionCommand _selectionCommand;

            public FakeEntity(long id, IEntitySelectionCommand selectionCommand)
            {
                Id = id;
                _selectionCommand = selectionCommand;
            }

            public long Id { get; }
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel => null;
            public PlayerType PlayerType => EmpireAtWar.Models.Factions.PlayerType.Player;

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                if (_selectionCommand is TCommand command)
                {
                    entityCommand = command;
                    return true;
                }

                entityCommand = default;
                return false;
            }
        }
    }
}
