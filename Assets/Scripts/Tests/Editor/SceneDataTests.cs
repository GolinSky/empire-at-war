using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Services.SceneService;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SceneDataTests
    {
        [Test]
        public void GetScene_MissingKey_Throws()
        {
            SceneData sceneData = ScriptableObject.CreateInstance<SceneData>();
            AssignEmptyScenes(sceneData);
            try
            {
                Assert.Throws<KeyNotFoundException>(() => sceneData.GetScene(SceneType.Loading));
            }
            finally
            {
                Object.DestroyImmediate(sceneData);
            }
        }

        // CreateInstance leaves the serialized wrapper unset; an asset without entries has an empty list.
        private static void AssignEmptyScenes(SceneData sceneData)
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo wrapperField = typeof(SceneData).GetField("scenesWrapper", PRIVATE_INSTANCE);
            object wrapper = Activator.CreateInstance(wrapperField.FieldType);
            FieldInfo listField = wrapperField.FieldType.GetField("keyValue", PRIVATE_INSTANCE);
            listField.SetValue(wrapper, Activator.CreateInstance(listField.FieldType));
            wrapperField.SetValue(sceneData, wrapper);
        }
    }
}
