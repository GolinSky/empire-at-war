using System.Collections.Generic;
using System.Linq;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Factory;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace WorkShop.LightWeightFramework.Game
{
    public interface IGameObserver
    {
        IFeatureFactory Factory { get; }
        IServiceHub ServiceHub { get; }
        IModelHub ModelHub { get; }
        
    }

    public interface IGame : IGameObserver
    {
        void LoadLevel(IGameLevelData levelData);
    }

    public class Game : IGame, ILateDisposable
    {
        private readonly IFeatureFactory factory;
        private readonly IGameContext gameContext;
        private readonly IServiceHub serviceHub;
        private readonly IModelHub modelHub;
        private IEnumerable<IController> controllers;
        private List<IView> views = new List<IView>();
        private readonly List<IService> services;
        IFeatureFactory IGameObserver.Factory => factory;
        IServiceHub IGameObserver.ServiceHub => serviceHub;
        IModelHub IGameObserver.ModelHub => modelHub;

        public Game(IFeatureFactory factory, IGameContext gameContext)
        {
            modelHub = new ModelHub(factory);
            this.factory = factory;
            this.gameContext = gameContext;
            
            serviceHub = new ServiceHub(gameContext.Services);
        }

        void IGame.LoadLevel(IGameLevelData levelData)
        {
            // if (SceneManager.GetActiveScene().buildIndex != levelData.SceneIndex) 
            // {
            //     SceneManager.LoadScene(levelData.SceneIndex); //move to lvl service
            // } 
            
            foreach (var service in services)
            {
                service.Init(this);
            }
            
            controllers = levelData.GetEntities(this);
            foreach (var entity in controllers)
            {
                entity.Init(this);
                var view = factory.CreateView(entity.Id);
                if (view != null)
                {
                    view.Init(entity.Model);
                    views.Add(view);
                    if (view is ICommandInvoker commandInvoker)
                    {
                        commandInvoker.SetCommand(entity.ConstructCommand());
                    }
                }
            }
        }


        public void LateDispose()
        {
            // foreach (var service in services)
            // {
            //     service.Release();
            // }
            //
            // foreach (var entity in controllers)
            // {
            //     entity.Release();
            // }
            //
            // foreach (var view in views)
            // {
            //     view.Release();
            // }
            
        }
    }
}