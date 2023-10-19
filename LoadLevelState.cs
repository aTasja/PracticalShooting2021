using System;
using System.Threading.Tasks;
using CodeBase.Hero;
using CodeBase.Infrastructure.Data;
using CodeBase.Infrastructure.Data.DataTypes;
using CodeBase.Infrastructure.Factory;
using CodeBase.Infrastructure.States;
using CodeBase.Logic;
using CodeBase.Logic.Level;
using CodeBase.Services.Audio;
using CodeBase.Services.Input;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.StaticData;
using CodeBase.UI;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows;
using UnityEngine;

namespace CodeBase.Infrastructure.State
{
    public class LoadLevelState : IPayloadedState<string>
    {
        private const string EnemySpawnerTag = "SpawnPoint";
        private const string HeroInitialPointTag = "HeroInitialPoint";

        private readonly IAudioService _audio;
        private readonly IGameFactory _gameFactory;
        private GameObject _hero;
        private GameObject _hud;

        private GameObject _initialPoint;
        private readonly IInputService _inputService;
        private Level _level;
        private readonly LoadingCurtain _loadingCurtain;
        private float _prelevelDelay;
        private PreLevelWindow _preLevelWindow;
        private readonly IPersistentProgressService _progressService;
        private readonly SceneLoader _sceneLoader;

        private readonly GameStateMachine _stateMachine;

        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowService;


        public LoadLevelState(GameStateMachine gameStateMachine,
                                SceneLoader sceneLoader,
                                LoadingCurtain loadingCurtain,
                                IGameFactory gameFactory,
                                IPersistentProgressService progressService,
                                IAudioService audioService,
                                IUIFactory uiFactory,
                                IWindowService windowService,
                                IInputService inputService)

        {
            _stateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _loadingCurtain = loadingCurtain;
            _gameFactory = gameFactory;
            _progressService = progressService;
            _audio = audioService;
            _uiFactory = uiFactory;
            _windowService = windowService;
            _inputService = inputService;
        }

        private async void CreateBars()
        {
            _level.SetTargetsBar((TargetsBar)await _windowService.Open(WindowID.TargetsBar));
            _level.SetDoubleAlphaBar((FadeBar)await _windowService.Open(WindowID.DoubleAlphaBar));
            _level.SetFriendlyFireBar((FadeBar)await _windowService.Open(WindowID.FriendlyFireBar));
        }

        private void InformProgressReaders()
        {
            foreach (ISavedProgressReader progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private async Task<GameObject> InitHeroAsync(Vector3 position) =>
          await _gameFactory.CreateHero(position);


        private async Task<GameObject> InitHudAsync()
        {
            GameObject hud = await _gameFactory.CreateHud();
            return hud;
        }
        private async void OnLoaded()
        {
            //_loadingCurtain.Show(true);
            await _uiFactory.CreateUIRoot();
            await InitLevel();
            InitLevelWorld();
            InformProgressReaders();
        }

        private async Task InitLevel()
        {
            //await Task.Delay(TimeSpan.FromSeconds(1));
            var levelStage = await _gameFactory.CreateLevelStage(_progressService.Progress.GetStage().stageType);
            _level = levelStage.GetComponent<Level>();
        }

        private async void InitLevelWorld()
        {
            _initialPoint = GameObject.FindWithTag(HeroInitialPointTag);
            _hero = await InitHeroAsync(_initialPoint.transform.position);
            _hud = await InitHudAsync();

            _level.Initialize(_inputService, _gameFactory, _audio, _hero, _uiFactory);
            CreateBars();

            ShowPreLevelWindow();

            _loadingCurtain.Hide();
        }

        private void NoButtonHandler()
        {
            _loadingCurtain.Show(false);
            _stateMachine.Enter<LoadProgressState>();
        }

        private void SetHeroToInitialPoint()
        {
            _hero.gameObject.SetActive(false);
            _hero.gameObject.transform.position = _initialPoint.transform.position;
            _hero.gameObject.SetActive(true);
        }

        private async void ShowPreLevelWindow()
        {
            _preLevelWindow = (PreLevelWindow)await _windowService.Open(WindowID.PrelevelWindow);
            _preLevelWindow.Construct(YesButtonHandler, NoButtonHandler, _audio);
        }

        private void YesButtonHandler()
        {
            if (GameConstants.stagesWithStandingMarks.Contains(_progressService.Progress.currentStageType)) SetHeroToInitialPoint();
            _prelevelDelay = UnityEngine.Random.Range(GameConstants.StandByTimeMin, GameConstants.StandByTimeMax);
            _preLevelWindow.ShowStandBy(_prelevelDelay, _level.StartLevel);
            _stateMachine.Enter<GameLoopState>();
        }

        public void Enter(string sceneName)
        {
            _gameFactory.Cleanup();
            _sceneLoader.Load(sceneName, OnLoaded);

            Debug.Log("GAME LEVEL STATE");
        }

        public void Exit()
        {

        }
    }
}