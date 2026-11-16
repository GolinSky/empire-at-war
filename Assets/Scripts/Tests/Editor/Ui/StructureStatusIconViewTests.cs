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
                    Assert.That(worldImages, Has.Length.EqualTo(3));
                    for (int i = 0; i < worldImages.Length; i++)
                    {
                        Assert.That(worldImages[i].sprite, Is.SameAs(mapImages[i].sprite));
                        Assert.That(worldImages[i].raycastTarget, Is.False);
                        Assert.That(mapImages[i].raycastTarget, Is.False);
                        Assert.That(mapImages[i].color.a, Is.EqualTo(0.5f));
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
    }
}
