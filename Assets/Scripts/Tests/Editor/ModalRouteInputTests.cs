using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class ModalRouteInputTests
    {
        private InputActionsProvider _provider;
        private InputLockService _inputLock;
        private UiCancelRouter _cancelRouter;
        private CancelInput _cancel;
        private PrefabUiService _ui;
        private PauseMenuRouteUiController _pause;
        private SaveGameRouteUiController _save;
        private BattleSaveActions _saveActions;
        private SkirmishStateModel _state;

        [SetUp]
        public void SetUp()
        {
            _provider = new InputActionsProvider();
            _inputLock = new InputLockService(_provider);
            _inputLock.Initialize();
            _cancel = new CancelInput();
            _cancelRouter = new UiCancelRouter(_cancel);
            _cancelRouter.Initialize();
            _ui = new PrefabUiService();
            _state = new SkirmishStateModel();
            _state.SetState(BattleState.Running);
            BattleDefinition battle = new BattleDefinition(GameMode.Skirmish, null,
                Array.Empty<BattlePlayer>(), BattleFeatures.None, OutcomeRule.Skirmish,
                BattleVictoryCondition.DestroyEnemyFleet, 0f);
            EmptyRepository repository = new EmptyRepository();
            BattleSaveService service = new BattleSaveService(battle, _state, null, null,
                null, repository, null, Array.Empty<IBattleSaveSource>());
            _saveActions = new BattleSaveActions(service, repository, null, battle);
            _save = new SaveGameRouteUiController(_ui, _cancelRouter, battle, repository,
                _saveActions, _inputLock);
            _save.Initialize();
            _pause = new PauseMenuRouteUiController(_ui, _cancelRouter, null, _state,
                null, _saveActions, _save, _inputLock);
            _pause.Initialize();
            _pause.UpdateState(BattleState.Running);
        }

        [TearDown]
        public void TearDown()
        {
            _save.LateDispose();
            _pause.LateDispose();
            _saveActions.Dispose();
            _cancelRouter.Dispose();
            _ui.Dispose();
            _inputLock.Dispose();
            UnityEngine.Object.DestroyImmediate(_provider.Actions.asset);
        }

        [Test]
        public void NestedSave_EscapeKeepsPauseLockedUntilResume()
        {
            _cancel.Press();
            AssertGameplayEnabled(false);
            _pause.OpenSaveGame();
            Assert.That(_save.IsFocused, Is.True);
            _cancel.Press();
            Assert.That(_save.IsFocused, Is.False);
            AssertGameplayEnabled(false);
            _cancel.Press();
            Assert.That(_pause.IsFocused, Is.False);
            AssertGameplayEnabled(true);
        }

        [Test]
        public void RepeatedOpen_DoesNotLeakEitherRouteLock()
        {
            _pause.Open();
            _pause.Open();
            _save.Open();
            _save.Open();
            _pause.Close();
            AssertGameplayEnabled(false);
            _save.Close();
            AssertGameplayEnabled(true);
            _pause.Close();
            _save.Close();
            AssertGameplayEnabled(true);
        }

        [Test]
        public void CancellingSaveName_KeepsBothModalLocks()
        {
            _pause.Open();
            _save.Open();
            _save.SaveAsNew();
            _cancel.Press();
            Assert.That(_save.IsFocused, Is.True);
            AssertGameplayEnabled(false);
            _cancel.Press();
            Assert.That(_save.IsFocused, Is.False);
            AssertGameplayEnabled(false);
            _pause.Close();
            AssertGameplayEnabled(true);
        }

        [Test]
        public void Teardown_ReleasesEachRouteWithoutUnlockingItsSibling()
        {
            _pause.Open();
            _save.Open();
            _save.LateDispose();
            AssertGameplayEnabled(false);
            _pause.LateDispose();
            AssertGameplayEnabled(true);
        }

        [Test]
        public void BattleEnd_ReleasesPauseLock()
        {
            _pause.Open();
            _state.SetState(BattleState.Ended);
            AssertGameplayEnabled(true);
            Assert.That(_pause.IsFocused, Is.False);
        }

        private void AssertGameplayEnabled(bool enabled)
        {
            Assert.That(_inputLock.IsLocked, Is.EqualTo(!enabled));
            Assert.That(_provider.Actions.Battle.enabled, Is.EqualTo(enabled));
            Assert.That(_provider.Actions.Camera.enabled, Is.EqualTo(enabled));
            Assert.That(_provider.Actions.Pointer.enabled, Is.True);
        }

        private sealed class CancelInput : ICancelInput
        {
            public event Action CancelPressed;
            public void Press() => CancelPressed?.Invoke();
        }

        private sealed class PrefabUiService : IUiService, IDisposable
        {
            private readonly List<GameObject> _roots = new List<GameObject>();

            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                string path = typeof(TUi) == typeof(PauseMenuUi)
                    ? "Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab"
                    : "Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                _roots.Add(root);
                return root.GetComponent<TUi>();
            }

            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }

            public void Dispose()
            {
                foreach (GameObject root in _roots) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private sealed class EmptyRepository : IBattleSaveRepository
        {
            public IReadOnlyList<BattleSaveHeader> List(GameMode mode) => Array.Empty<BattleSaveHeader>();
            public void Save(BattleSave save) => throw new NotSupportedException();
            public BattleSave Load(GameMode mode, BattleSaveSlot slot) => throw new NotSupportedException();
            public void Delete(GameMode mode, BattleSaveSlot slot) => throw new NotSupportedException();
        }
    }
}
