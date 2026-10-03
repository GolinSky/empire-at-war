using System.Collections.Generic;
using UnityEngine;
using EmpireAtWar.Mvc;
using UnityEngine.SceneManagement;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneModelObserver : IModelObserver
    {

    }

    [CreateAssetMenu(fileName = nameof(SceneData), menuName = "Data/SceneData")]
    public class SceneData : Data, ISceneModelObserver
    {
        private const SceneType LOADING_SCENE_TYPE = SceneType.Loading;

        [SerializeField] private DictionaryWrapper<SceneType, SceneReference> scenesWrapper;

        private Dictionary<SceneType, SceneReference> SceneDictionary => scenesWrapper.Dictionary;

        public SceneReference GetScene(SceneType sceneType)
        {
            if (SceneDictionary.TryGetValue(sceneType, out SceneReference scene))
            {
                return scene;
            }

            throw new KeyNotFoundException($"{nameof(SceneData)} has no scene for {nameof(SceneType)}.{sceneType}.");
        }

        public SceneType GetCurrentScene()
        {
            return GetSceneType(SceneManager.GetActiveScene().path);
        }

        public SceneReference GetLoadScene()
        {
            return GetScene(LOADING_SCENE_TYPE);
        }

        public SceneType GetSceneType(Scene scene)
        {
            return GetSceneType(scene.path);
        }

        public bool IsLoadingScene(Scene scene)
        {
            SceneType sceneType = GetSceneType(scene.path);
            return sceneType == LOADING_SCENE_TYPE;
        }

        private SceneType GetSceneType(string path)
        {
            foreach (var keyValuePair in SceneDictionary)
            {
                if (keyValuePair.Value.ScenePath == path)
                {
                    return keyValuePair.Key;
                }
            }
            return SceneType.Undefined;
        }

    }
}