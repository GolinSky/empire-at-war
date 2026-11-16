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
        public void SharedArtworkBindsEveryStructureAndState()
        {
            StructureStatusIconArtwork art = AssetDatabase.LoadAssetAtPath<MiniMapData>(DATA_PATH).StructureStatusIcons;
            Assert.That(art, Is.Not.Null);
            foreach (StructureStatusIconKind kind in Enum.GetValues(typeof(StructureStatusIconKind)))
                Assert.That(art.GetGlyph(kind), Is.Not.Null, kind.ToString());
            foreach (StructureStatusIconState state in Enum.GetValues(typeof(StructureStatusIconState)))
                Assert.That(art.GetBadge(state), Is.Not.Null, state.ToString());
            Assert.That(art.GetBadge(StructureStatusIconState.Captured), Is.Not.SameAs(art.GetBadge(StructureStatusIconState.Built)));
            Assert.That(art.GetBadge(StructureStatusIconState.Built), Is.Not.SameAs(art.GetBadge(StructureStatusIconState.Destroyed)));
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
                foreach (StructureStatusIconKind kind in Enum.GetValues(typeof(StructureStatusIconKind)))
                foreach (StructureStatusIconState state in Enum.GetValues(typeof(StructureStatusIconState)))
                {
                    world.SetAppearance(art, kind, state);
                    map.SetAppearance(art, kind, state);
                    world.Display(true, 1f, Vector2.one);
                    map.Display(true, 0.5f, Vector2.zero);
                    Image[] worldImages = world.Root.GetComponentsInChildren<Image>();
                    Image[] mapImages = map.Root.GetComponentsInChildren<Image>();
                    Assert.That(worldImages, Has.Length.EqualTo(6));
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
                view.SetAppearance(art, StructureStatusIconKind.Mining, StructureStatusIconState.Captured);
                view.Display(true, 0.8f, Vector2.zero);
                view.SetAppearance(art, StructureStatusIconKind.Mining, StructureStatusIconState.Built);
                view.Display(true, 0f, Vector2.zero);
                Image[] images = view.Root.GetComponentsInChildren<Image>();
                Assert.That(images[2].sprite, Is.SameAs(art.GetBadge(StructureStatusIconState.Captured)));
                Assert.That(images[5].sprite, Is.SameAs(art.GetBadge(StructureStatusIconState.Built)));
                Assert.That(images[2].color.a, Is.EqualTo(0.8f));
                Assert.That(images[5].color.a, Is.Zero);
                view.Display(true, 0.75f, Vector2.zero, 0.15f);
                Assert.That(images[2].color.a, Is.EqualTo(0.4f).Within(0.00001f));
                Assert.That(images[5].color.a, Is.EqualTo(0.75f));
                view.Display(true, 1f, Vector2.zero, 0.16f);
                Assert.That(images[2].enabled, Is.False);
                view.Display(false, 0f, Vector2.zero);
                view.Display(true, 0f, Vector2.zero);
                Assert.That(images[5].color.a, Is.Zero, "A reused view must not retain its previous visible alpha.");
            }
            finally
            {
                view.Release();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}
