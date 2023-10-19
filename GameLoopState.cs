using CodeBase.Infrastructure.State;
using CodeBase.Logic;
using CodeBase.Logic.Level;
using CodeBase.Services.AssetManagement;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.SaveLoad;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows;
using CodeBase.UI.Windows.UIElements;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameLoopState : IState
    {

        private Level _level;
        private readonly LoadingCurtain _loadingCurtain;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadService;
        private readonly GameStateMachine _stateMachine;
        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowService;

        public GameLoopState(GameStateMachine stateMachine, IWindowService windowService,
          IPersistentProgressService progressService, IUIFactory uiFactory, LoadingCurtain loadingCurtain, ISaveLoadService saveLoadService)
        {
            _stateMachine = stateMachine;
            _windowService = windowService;
            _progressService = progressService;
            _uiFactory = uiFactory;
            _loadingCurtain = loadingCurtain;
            _saveLoadService = saveLoadService;
        }

        private void LoadMainMenu()
        {
            _stateMachine.Enter<MenuMainState>();
        }

        private void LoadStagesMenu()
        {
            _stateMachine.Enter<MenuStagesState>();
        }


        private async void StageIsOver()
        {

            if (!_level.IsDisqualified) {
                _saveLoadService.SaveProgress();
                var gameOverWindow = (GameOverWindow)await _windowService.Open(WindowID.GameOver);
                gameOverWindow.Construct(_progressService.Progress, _level.levelData, _uiFactory, LoadStagesMenu);
            } else {
                var disqualifiedGameObject = await _uiFactory.CreateUiElementAsync(AssetAddress.DisqualifyUIPrefabAddress, null);
                DisqualifyUI disqualifyUI = disqualifiedGameObject.GetComponent<DisqualifyUI>();
                disqualifyUI.Construct(_progressService.Progress, _uiFactory, LoadMainMenu);
                await disqualifyUI.CreateAchWaterfall();
                _progressService.Progress.Disqualify();
                _saveLoadService.SaveProgress();
            }
            

        }

        public void Enter()
        {
            Debug.Log("GAME LOOP STATE");

            _level = GameObject.FindWithTag("Level").GetComponent<Level>();
            _level.OnStageIsOverAction += StageIsOver;

            if (_progressService.Progress.currentStageType == Data.DataTypes.StageType.intro) {
                ShowIntroFinger();
            }
        }
        private async void ShowIntroFinger() => 
            await _uiFactory.CreateUiElementAsync(AssetAddress.FingerIntroAddress, null);

        public void Exit()
        {
            _loadingCurtain.Show(false);
        }
    }
}