using CodeBase.Firebase;
using CodeBase.Infrastructure.Factory;
using CodeBase.Infrastructure.State;
using CodeBase.Services;
using CodeBase.Services.AssetManagement;
using CodeBase.Services.Audio;
using CodeBase.Services.GoogleSighIn;
using CodeBase.Services.Input;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.SaveLoad;
using CodeBase.Services.Server;
using CodeBase.Services.StaticData;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
  public class BootstrapState : IState
  {
    private const string InitialSceneName = "Boot";
    private readonly GameStateMachine _stateMachine;
    private readonly SceneLoader _sceneLoader;
    private readonly AllServices _services;
    private readonly ICoroutineRunner _coroutineRunner;

    public BootstrapState(GameStateMachine stateMachine, SceneLoader sceneLoader, AllServices services, ICoroutineRunner coroutineRunner)
    {
      _stateMachine = stateMachine;
      _sceneLoader = sceneLoader;
      _services = services;
      _coroutineRunner = coroutineRunner;
      
      
      RegisterServices(); 
    }

    public void Enter()
    {
      _sceneLoader.Load(InitialSceneName, onLoaded: EnterLoadLevel);
    }

    public void Exit()
    {
    }
    
    private void RegisterServices()
    {
      RegisterStaticData();
      RegisterAdsService();

      _services.RegisterSingle<IGameStateMachine>(_stateMachine);
      _services.RegisterSingle<IInputService>(InputService());
      _services.RegisterSingle<IAndroidToastMessageService>(new AndroidToastMessageService());
      //_services.RegisterSingle<IRandomService>(new RandomService());
      _services.RegisterSingle<IAssetProvider>(new AssetProvider());
      _services.RegisterSingle<IAudioService>(new AudioService(_services.Single<IAssetProvider>()));
      _services.RegisterSingle<IPersistentProgressService>(new PersistentProgressService());
      
      _services.RegisterSingle<IGameFactory>(new GameFactory(
      
        _services.Single<IAssetProvider>(),
        _services.Single<IAudioService>(),
        _services.Single<IPersistentProgressService>(), 
        _services.Single<IStaticDataService>()));/*, 
        _services.Single<IRandomService>(), 
                _services.Single<IWindowService>()
        ));*/


      _services.RegisterSingle<IUIFactory>(new UIFactory(
        _services.Single<IAssetProvider>(),
        _services.Single<IStaticDataService>(),
        _services.Single<IPersistentProgressService>(),
        _services.Single<IAudioService>()));
        //_services.Single<IAdsService>()
      
      
      _services.RegisterSingle<IWindowService>(new WindowService(
        _services.Single<IUIFactory>()
      ));

      _services.RegisterSingle<ISaveLoadService>(new SaveLoadService(
        progressService: _services.Single<IPersistentProgressService>(), 
        _services.Single<IGameFactory>()
      ));
      
      _services.RegisterSingle<IPlayerCredentialsService>(new PlayerCredentialsService());
      _services.RegisterSingle<IGoogleSignInService>( new GoogleSignInService());
      
      _services.RegisterSingle<IServerService>(
        new ServerService(_coroutineRunner, 
          _services.Single<IPlayerCredentialsService>(),
          _services.Single<IPersistentProgressService>()));


    }
    
    private void RegisterAdsService()
    {
      var adsService = new AdsService();
      adsService.Initialize();
      _services.RegisterSingle<IAdsService>(adsService);
    }


    private void RegisterStaticData()
    {
      IStaticDataService staticData = new StaticDataService();
      staticData.LoadStaticData();
      _services.RegisterSingle(staticData);
    }

    private void EnterLoadLevel()
    {
      _stateMachine.Enter<LoadProgressState>();
    }
    
    private static IInputService InputService()
    {
      if (Application.isEditor)
        return new StandaloneInputService();
      else
        return new MobileInputService();
    }

  }
}