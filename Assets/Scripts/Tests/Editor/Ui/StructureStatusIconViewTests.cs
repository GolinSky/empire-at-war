using System;
using EmpireAtWar.Entities.StructureStatusIcons;
using EmpireAtWar.Models.MiniMap;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class StructureStatusIconViewTests
    {
        private const string DATA_PATH = "Assets/Settings/Data/Models/MiniMap/MiniMapData.asset";

        [Test]
        public void SharedArtworkBindsThreeDistinctStateSprites()
        {
            StructureStatusIconArtwork art = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH).StructureStatusIcons;
            Assert.That(art, Is.Not.Null);
            var states = (StructureStatusIconState[])Enum.GetValues(typeof(StructureStatusIconState));
            Assert.That(states, Has.Length.EqualTo(3));
            var sprites = Array.ConvertAll(states, art.GetSprite);
            Assert.That(sprites, Has.None.Null);
            Assert.That(sprites, Is.Unique);
        }

        [Test]
        public void PassiveViewsShareArtworkWithoutCanvasRaycastsOrPerIconUpdates()
        {
            var parent = new GameObject("StructureIconTest", typeof(RectTransform));
            StructureStatusIconView world = StructureStatusIconView.Create(parent.transform, 48f, 0);
            StructureStatusIconView map = StructureStatusIconView.Create(parent.transform, 26f, 0);
            try
            {
                StructureStatusIconArtwork art = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH).StructureStatusIcons;
                foreach (StructureStatusIconState state in Enum.GetValues(typeof(StructureStatusIconState)))
                {
                    world.SetAppearance(art, state);
                    map.SetAppearance(art, state);
                    world.Display(true, 1f, Vector2.one);
                    map.Display(true, 0.5f, Vector2.zero);
                    Image[] worldImages = world.Root.GetComponentsInChildren<Image>();
                    Image[] mapImages = map.Root.GetComponentsInChildren<Image>();
                    Assert.That(worldImages, Has.Length.EqualTo(2));
                    Assert.That(mapImages, Has.Length.EqualTo(2));
                    Assert.That(worldImages[1].sprite, Is.SameAs(art.GetSprite(state)));
                    for (int i = 0; i < worldImages.Length; i++)
                    {
                        Assert.That(worldImages[i].sprite, Is.SameAs(mapImages[i].sprite));
                        Assert.That(worldImages[i].raycastTarget, Is.False);
                        Assert.That(mapImages[i].raycastTarget, Is.False);
                        if (mapImages[i].enabled) Assert.That(mapImages[i].color.a, Is.EqualTo(0.5f));
                    }
                }
                Assert.That(parent.GetComponentsInChildren<Canvas>(), Is.Empty);
                world.Display(false, 0f, default);
                Assert.That(world.Root.gameObject.activeSelf, Is.False);
                world.Display(true, 1f, Vector2.one * 10f);
                Assert.That(world.Root.anchoredPosition, Is.EqualTo(Vector2.one * 10f));
            }
            finally
            {
                world.Release();
                map.Release();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ReplacingVisibleStateFadesOldArtworkWhileNewArtworkFadesIn()
        {
            var parent = new GameObject("StructureIconTransitionTest", typeof(RectTransform));
            StructureStatusIconView view = StructureStatusIconView.Create(parent.transform, 48f, 0);
            try
            {
                StructureStatusIconArtwork art = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH).StructureStatusIcons;
                view.SetAppearance(art, StructureStatusIconState.Captured);
                view.Display(true, 0.8f, Vector2.zero);
                view.SetAppearance(art, StructureStatusIconState.BuildAvailable);
                view.Display(true, 0f, Vector2.zero);
                Image[] images = view.Root.GetComponentsInChildren<Image>();
                Assert.That(images[0].sprite, Is.SameAs(art.GetSprite(StructureStatusIconState.Captured)));
                Assert.That(images[1].sprite, Is.SameAs(art.GetSprite(StructureStatusIconState.BuildAvailable)));
                Assert.That(images[0].color.a, Is.EqualTo(0.8f));
                Assert.That(images[1].color.a, Is.Zero);
                view.Display(true, 0.75f, Vector2.zero, 0.15f);
                Assert.That(images[0].color.a, Is.EqualTo(0.4f).Within(0.00001f));
                Assert.That(images[1].color.a, Is.EqualTo(0.75f));
                view.Display(true, 1f, Vector2.zero, 0.16f);
                Assert.That(images[0].enabled, Is.False);
                view.Display(false, 0f, Vector2.zero);
                view.Display(true, 0f, Vector2.zero);
                Assert.That(images[1].color.a, Is.Zero, "A reused view must not retain its previous visible alpha.");
            }
            finally
            {
                view.Release();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}
