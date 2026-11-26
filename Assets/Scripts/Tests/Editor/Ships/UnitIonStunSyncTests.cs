using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.TeamColor;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitIonStunSyncTests
    {
        private CombatModifiers _modifiers;
        private RenderState _renderState;
        private UnitIonStunSync _sync;

        [SetUp]
        public void SetUp()
        {
            _modifiers = new CombatModifiers();
            _renderState = new RenderState();
            _sync = new UnitIonStunSync(_modifiers, _renderState);
        }

        [Test]
        public void Initialize_ShowsCurrentIonState()
        {
            _modifiers.SetIonDisabled(true);

            _sync.Initialize();

            Assert.That(_renderState.IonStunned, Is.True);
        }

        [Test]
        public void IonPulse_StunsUntilLastDisableEnds()
        {
            _sync.Initialize();
            Assert.That(_renderState.IonStunned, Is.False);

            _modifiers.AddIonPulseDisable();
            _modifiers.AddIonPulseDisable();
            Assert.That(_renderState.IonStunned, Is.True);

            _modifiers.RemoveIonPulseDisable();
            Assert.That(_renderState.IonStunned, Is.True);

            _modifiers.RemoveIonPulseDisable();
            Assert.That(_renderState.IonStunned, Is.False);
        }

        [Test]
        public void LateDispose_StopsFollowingModifiers()
        {
            _sync.Initialize();
            _sync.LateDispose();

            _modifiers.SetIonDisabled(true);

            Assert.That(_renderState.IonStunned, Is.False);
        }

        private sealed class RenderState : IUnitRenderStateComponent
        {
            public bool IonStunned { get; private set; }

            public uint WreckUserValue => 0u;

            public void SetHighlight(UnitHighlight flags) { }

            public void SetHullDamage(float hullPercentage) { }

            public void SetIonStunned(bool stunned) => IonStunned = stunned;
        }
    }
}
