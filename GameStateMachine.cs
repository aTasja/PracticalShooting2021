using System;
using System.Collections.Generic;
using CodeBase.Firebase;
using CodeBase.Infrastructure.Factory;
using CodeBase.Infrastructure.States;
using CodeBase.Logic;
using CodeBase.Services;
using CodeBase.Services.Audio;
using CodeBase.Services.Input;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.SaveLoad;
using CodeBase.Services.Server;
using CodeBase.Services.StaticData;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;

namespace CodeBase.Infrastructure.State
{
  public class GameStateMachine : IGameStateMachine
  {
    private readonly Dictionary<Type, IExitableState> _states;
    private IExitableState _activeState;

    public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain, AllServices services, ICoroutineRunner runner)
    {
      _states = new Dictionary<Type, IExitableState>
      {
        [typeof(BootstrapState)] = new BootstrapState(this, sceneLoader, services, runner),
        
        [typeof(LoadProgressState)] = new LoadProgressState(this, 
          services.Single<IGameFactory>(),
          services.Single<IPersistentProgressService>(), 
          services.Single<ISaveLoadService>(),
          services.Single<IStaticDataService>()),
        
        [typeof(MenuMainState)] = new MenuMainState(this, 
          sceneLoader, 
          services.Single<IGameFactory>(), 
          services.Single<IPersistentProgressService>(),
          services.Single<IUIFactory>(),
          services.Single<IWindowService>(), loadingCurtain,
          services.Single<IPlayerCredentialsService>(),
          services.Single<IGoogleSignInService>(),
          services.Single<IServerService>(),
          services.Single<IAudioService>(),
          services.Single<IAdsService>()),
        
        [typeof(MenuStagesState)] = new MenuStagesState(this, 
          sceneLoader, 
          services.Single<IGameFactory>(), 
          services.Single<IPersistentProgressService>(),
          services.Single<IUIFactory>(),
          services.Single<IWindowService>(), loadingCurtain, services.Single<IAudioService>()),
        
        [typeof(LoadLevelState)] = new LoadLevelState(this, sceneLoader, loadingCurtain, 
          services.Single<IGameFactory>(), 
          services.Single<IPersistentProgressService>(),
          services.Single<IAudioService>(),
          services.Single<IUIFactory>(),
          services.Single<IWindowService>(),
          services.Single<IInputService>()),
        
        [typeof(MenuStatisticState)] = new MenuStatisticState(this,
          services.Single<IPersistentProgressService>(),
          services.Single<IUIFactory>(),
          services.Single<IWindowService>(), loadingCurtain, 
          services.Single<IAudioService>(), 
          services.Single<IServerService>(),
          services.Single<IStaticDataService>(),
          services.Single<IAdsService>()),
        
        [typeof(MenuAboutState)] = new MenuAboutState(this, 
          services.Single<IUIFactory>(),
          services.Single<IWindowService>(), loadingCurtain, 
          services.Single<IAudioService>()),
        
        
        [typeof(GameLoopState)] = new GameLoopState(this, 
          services.Single<IWindowService>(), 
          services.Single<IPersistentProgressService>(),
          services.Single<IUIFactory>(), loadingCurtain,
          services.Single<ISaveLoadService>()),
      };
    }
    
    public void Enter<TState>() where TState : class, IState
    {
      IState state = ChangeState<TState>();
      state.Enter();
    }

    public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
    {
      TState state = ChangeState<TState>();
      state.Enter(payload);
    }

    private TState ChangeState<TState>() where TState : class, IExitableState
    {
      _activeState?.Exit();
      
      TState state = GetState<TState>();
      _activeState = state;
      
      return state;
    }

    private TState GetState<TState>() where TState : class, IExitableState => 
      _states[typeof(TState)] as TState;
  }
}