using System.Collections.Generic;
using EmpireAtWar.Models.Planet;
using UnityEngine;
using LightWeightFramework.Model;
using UnityEngine.SceneManagement;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneModelObserver : IModelObserver
    {

    }

    [CreateAssetMenu(fileName = "SceneModel", menuName = "Model/SceneModel")]
    public class SceneModel : Model, ISceneModelObserver
    {
        private const SceneType LOADING_SCENE_TYPE = SceneType.Loading;
        
        [SerializeField] private DictionaryWrapper<SceneType, SceneReference> scenesWrapper;
        private Dictionary<SceneType, SceneReference> SceneDictionary => scenesWrapper.Dictionary;
        
        private readonly Dictionary<PlanetType, SceneType> _planetScenes = new Dictionary<PlanetType, SceneType>
        {
            {  PlanetType.Coruscant, SceneType.Coruscant },
            {  PlanetType.Kamino, SceneType.Kamino }
        };

        public SceneReference GetScene(SceneType sceneType)
        {
            if (SceneDictionary.ContainsKey(sceneType))
            {
                return SceneDictionary[sceneType];
            }

            return null;
        }
        
        public SceneReference GetScene(PlanetType planetType)
        {
            if (!_planetScenes.TryGetValue(planetType, out SceneType sceneType))
            {
                Debug.LogError($"No scene type for {planetType}");
            }

            return GetScene(sceneType);
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