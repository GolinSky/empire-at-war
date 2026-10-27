using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SelectionMarkerTests
    {
        private const string SELECTION_DATA_PATH =
            "Assets/Settings/Data/Selection/SharedSelectionData.asset";

        private GameObject _root;
        private SelectionComponent _selection;
        private SpriteRenderer _renderer;
        private SelectionServiceStub _service;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Selection marker test");
            _selection = _root.AddComponent<SelectionComponent>();
            GameObject anchor = new GameObject("Selection anchor");
            anchor.transform.SetParent(_root.transform, false);
            anchor.transform.localPosition = new Vector3(3f, 4f, 5f);
            GameObject marker = new GameObject("Selection marker");
            marker.transform.SetParent(anchor.transform, false);
            _renderer = marker.AddComponent<SpriteRenderer>();
            SerializedObject serialized = new SerializedObject(_selection);
            serialized.FindProperty("selectionRoot").objectReferenceValue = anchor.transform;
            serialized.FindProperty("selectedRenderer").objectReferenceValue = _renderer;
            serialized.FindProperty("ringSize").vector2Value = new Vector2(12f, 8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _service = new SelectionServiceStub();
            SharedSelectionData data = AssetDatabase.LoadAssetAtPath<SharedSelectionData>(SELECTION_DATA_PATH);
            typeof(SelectionComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_selection, new object[]
                {
                    new SelectionModel(), data, TestPlayers.Human,
                    TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), _service
                });
            _selection.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _selection.Release();
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void SelectionCountChanges_ResizeExistingMarkerWithoutMovingHealthAnchor()
        {
            _service.SetCount(1);
            _selection.SetActive(true);
            Vector3 singleScale = _renderer.transform.localScale;
            Vector3 anchor = _selection.WorldPosition;
            Assert.That(singleScale.x * _renderer.sprite.bounds.size.x,
                Is.EqualTo(12f * 0.65f).Within(0.0001f));
            Assert.That(singleScale.y * _renderer.sprite.bounds.size.y,
                Is.EqualTo(8f * 0.65f).Within(0.0001f));

            _service.SetCount(2);

            Assert.That(_renderer.enabled, Is.True);
            Assert.That(_renderer.transform.localScale.x, Is.EqualTo(singleScale.x * (0.6f / 0.65f)).Within(0.0001f));
            Assert.That(_renderer.transform.localScale.y, Is.EqualTo(singleScale.y * (0.6f / 0.65f)).Within(0.0001f));
            Assert.That(_renderer.transform.localScale.z, Is.EqualTo(1f));
            Assert.That(_selection.WorldPosition, Is.EqualTo(anchor));

            _service.SetCount(1);

            Assert.That(_renderer.enabled, Is.True);
            Assert.That(_renderer.transform.localScale, Is.EqualTo(singleScale));
            Assert.That(_selection.WorldPosition, Is.EqualTo(anchor));
        }

        [TestCase(0f, 45f, 0f)]
        [TestCase(15f, 130f, 10f)]
        public void SelectedMarker_KeepsWorldRotationWhenRootTurns(float pitch, float yaw, float roll)
        {
            _selection.SetActive(true);
            _root.transform.rotation = Quaternion.Euler(pitch, yaw, roll);
            Vector3 anchor = _selection.WorldPosition;
            Vector3 markerPosition = _renderer.transform.position;
            typeof(SelectionComponent).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_selection, null);

            Assert.That(Quaternion.Angle(_renderer.transform.rotation, Quaternion.Euler(90f, 0f, 0f)),
                Is.LessThan(0.01f));
            Assert.That(_selection.WorldPosition, Is.EqualTo(anchor));
            Assert.That(_renderer.transform.position, Is.EqualTo(markerPosition));
            Assert.That(Quaternion.Angle(_root.transform.rotation, Quaternion.Euler(pitch, yaw, roll)),
                Is.LessThan(0.01f));

            _renderer.transform.hasChanged = false;
            typeof(SelectionComponent).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_selection, null);
            Assert.That(_renderer.transform.hasChanged, Is.False, "Stationary markers must not rewrite their transform.");
        }

        [Test]
        public void HiddenMarker_SkipsRotationWorkAndRealignsImmediatelyWhenSelected()
        {
            _selection.SetActive(false);
            _root.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
            Quaternion hiddenRotation = _renderer.transform.rotation;
            typeof(SelectionComponent).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_selection, null);
            Assert.That(_renderer.transform.rotation, Is.EqualTo(hiddenRotation));

            _selection.SetActive(true);

            Assert.That(Quaternion.Angle(_renderer.transform.rotation, Quaternion.Euler(90f, 0f, 0f)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void ReleaseAndRespawn_RestoreScaleAndDoNotDuplicateObservers()
        {
            Vector3 singleScale = _renderer.transform.localScale;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.That(_service.ObserverCount, Is.EqualTo(1));
                _selection.SetActive(true);
                _service.SetCount(2);
                _selection.Release();
                Assert.That(_renderer.enabled, Is.False);
                Assert.That(_service.ObserverCount, Is.Zero);
                Vector3 releasedScale = _renderer.transform.localScale;
                _service.SetCount(0);
                Assert.That(_renderer.transform.localScale, Is.EqualTo(releasedScale));

                _selection.Respawn();

                Assert.That(_service.ObserverCount, Is.EqualTo(1));
                Assert.That(_renderer.enabled, Is.False);
                Assert.That(_renderer.transform.localScale, Is.EqualTo(singleScale));
            }
        }

        [Test]
        public void SharedSelectionAssets_UseSupportedBracketShaderAndFullRectangleSprite()
        {
            SharedSelectionData data = AssetDatabase.LoadAssetAtPath<SharedSelectionData>(SELECTION_DATA_PATH);
            Assert.That(data.SelectionMaterial, Is.Not.Null);
            Shader shader = data.SelectionMaterial.shader;
            Assert.That(shader.name, Is.EqualTo("EmpireAtWar/UI/Selection Brackets"));
            Assert.That(shader.isSupported, Is.True);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(data.SelectionMaterial));
            Assert.That(_renderer.sprite, Is.SameAs(data.SelectionSprite));
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(data.SelectionSprite));
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(data.SelectionSprite.vertices.Length, Is.EqualTo(4));
        }

        internal sealed class SelectionServiceStub : ISelectionService, ISelectionContext, ISelectionSubject
        {
            private readonly List<IObserver<ISelectionSubject>> _observers =
                new List<IObserver<ISelectionSubject>>();

            public string Id => nameof(SelectionServiceStub);
            public ISelectionContext PlayerSelectionContext => this;
            public ISelectionContext OtherSelectionContext => throw new NotSupportedException();
            public SelectionScope UpdatedScope => SelectionScope.Local;
            public bool IsPruningSelection => false;
            public IEntity Entity => throw new NotSupportedException();
            public IReadOnlyList<IEntity> Entities => throw new NotSupportedException();
            public IEntitySelectionFacade SelectionFacade => throw new NotSupportedException();
            public SelectionType SelectionType => SelectionType.Ship;
            public bool HasSelectable => Count > 0;
            public int Count { get; private set; }
            public SelectionScope Scope => SelectionScope.Local;
            public int ObserverCount => _observers.Count;

            public void SetCount(int count)
            {
                Count = count;
                foreach (IObserver<ISelectionSubject> observer in _observers)
                    observer.UpdateState(this);
            }

            public bool Contains(IEntity entity) => throw new NotSupportedException();
            public void RemoveSelectable(ISelectionContext context) => throw new NotSupportedException();
            public void SelectCurrentUnitsByType(UnitTypeId unitTypeId) => throw new NotSupportedException();
            public void AddObserver(IObserver<ISelectionSubject> observer) => _observers.Add(observer);
            public void RemoveObserver(IObserver<ISelectionSubject> observer) => _observers.Remove(observer);
        }
    }
}
