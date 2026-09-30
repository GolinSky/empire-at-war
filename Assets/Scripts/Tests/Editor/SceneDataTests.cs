using System.Collections.Generic;
using EmpireAtWar.Services.SceneService;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SceneDataTests
    {
        [Test]
        public void GetScene_MissingKey_Throws()
        {
            SceneData sceneData = ScriptableObject.CreateInstance<SceneData>();
            try
            {
                Assert.Throws<KeyNotFoundException>(() => sceneData.GetScene(SceneType.Loading));
            }
            finally
            {
                Object.DestroyImmediate(sceneData);
            }
        }
    }
}
