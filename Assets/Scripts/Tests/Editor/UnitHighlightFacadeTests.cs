using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Selection;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitHighlightFacadeTests
    {
        [Test]
        public void HoverAndSelection_ClearIndependently()
        {
            SelectionModel selection = new SelectionModel();
            RenderState renderState = new RenderState();
            UnitHighlightFacade facade = new UnitHighlightFacade(selection, renderState);
            facade.Initialize();

            facade.SetHovered(true);
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.Hovered));
            selection.IsSelected = true;
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.Hovered | UnitHighlight.Selected));
            facade.SetHovered(false);
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.Selected));
            facade.SetHovered(true);
            selection.IsSelected = false;
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.Hovered));
            facade.SetHovered(false);
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.None));
            facade.LateDispose();
        }

        [Test]
        public void Initialize_UsesExistingSelection()
        {
            SelectionModel selection = new SelectionModel { IsSelected = true };
            RenderState renderState = new RenderState();
            UnitHighlightFacade facade = new UnitHighlightFacade(selection, renderState);
            facade.Initialize();
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.Selected));
            facade.LateDispose();
        }

        [Test]
        public void PoolAndDispose_ClearBothFlags_AndDisposeUnsubscribes()
        {
            SelectionModel selection = new SelectionModel();
            RenderState renderState = new RenderState();
            UnitHighlightFacade facade = new UnitHighlightFacade(selection, renderState);
            facade.Initialize();
            selection.IsSelected = true;
            facade.SetHovered(true);
            facade.Despawn();
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.None));
            selection.IsSelected = true;
            facade.SetHovered(true);
            facade.Respawn();
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.None));
            selection.IsSelected = true;
            facade.SetHovered(true);
            facade.LateDispose();
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.None));
            selection.IsSelected = true;
            Assert.That(renderState.Highlight, Is.EqualTo(UnitHighlight.None));
        }

        private sealed class RenderState : IUnitRenderStateComponent
        {
            public UnitHighlight Highlight { get; private set; }

            public void SetHighlight(UnitHighlight flags) => Highlight = flags;
        }
    }
}
