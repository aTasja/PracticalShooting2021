using CodeBase.Infrastructure.Data;
using CodeBase.Infrastructure.Factory;
using CodeBase.Logic;
using CodeBase.Services.Audio;
using CodeBase.Services.PersistentProgress;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeBase.Infrastructure.State
{
    public class MenuStagesState:IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly SceneLoader _sceneLoader;
        private StagesWindow _stagesWindow;
        private readonly IPersistentProgressService _progress;
        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowsService;
        private readonly LoadingCurtain _loadingCurtain;

        private readonly IGameFactory _factory;
        private readonly IAudioService _audioService;

        private GameObject _macroStageGO;

        public MenuStagesState(GameStateMachine gameStateMachine, SceneLoader sceneLoader, 
            IGameFactory factory, IPersistentProgressService progress,  IUIFactory uiFactory, 
            IWindowService windowsService, LoadingCurtain loadingCurtain, IAudioService audioService)
        {
            _stateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _factory = factory;
            _progress = progress;
            _uiFactory = uiFactory;
            _windowsService = windowsService;
            _loadingCurtain = loadingCurtain;
            _audioService = audioService;

        }
        
        public void Enter()
        {
            Debug.Log("GAME STAGES STATE");
            if (SceneManager.GetActiveScene().name != GameConstants.MenuSceneName) {
                _sceneLoader.Load(GameConstants.MenuSceneName, onLoaded: OpenMenu);
            } else
                OpenStagesMenu();
        }
        
        private void EscapeButtonHandlerStagesMenu()
        {
            if (_macroStageGO != null) {
                _stagesWindow.CloseMacroSpriteClicked();
            }
            else {
                _stagesWindow.Kill();
                _stateMachine.Enter<MenuMainState>();
            }
        }

        private async void OpenMenu()
        {
            _factory.CreateAudioSource();
            await _uiFactory.CreateUIRoot();
            OpenStagesMenu();
        }

        private async void OpenStagesMenu()
        {
            _stagesWindow = (StagesWindow) await _windowsService.Open(WindowID.Stages);
            _stagesWindow.Construct(_progress.Progress, _audioService, _loadingCurtain);
            SetupStagesWindow();
            await _uiFactory.SubscribeToEscape(EscapeButtonHandlerStagesMenu);
        }
        
        private void SetupStagesWindow()
        { 
            _stagesWindow.FillContainer();
            _stagesWindow.OnStageClickedAction += () => {
                _stagesWindow.OnStageClickedAction -= OpenLevel;
                _stagesWindow.Kill();
                OpenLevel();
            };
            _stagesWindow.OnMacroStageOpened += o => {

                _macroStageGO = o;
            };
            _stagesWindow.OnMacroStageClosed += () => _macroStageGO = null;
        }

        private void OpenLevel() => 
            _stateMachine.Enter<LoadLevelState, string>(_progress.Progress.WorldData.levelSceneName);

        public void Exit() => 
            _uiFactory.UnSubscribeToEscape(EscapeButtonHandlerStagesMenu);
    }
}
