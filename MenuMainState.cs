using CodeBase.Firebase;
using CodeBase.Infrastructure.Data;
using CodeBase.Infrastructure.Factory;
using CodeBase.Logic;
using CodeBase.Services;
using CodeBase.Services.Audio;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.Server;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows;
using Unity.VisualScripting;
using UnityEngine;

namespace CodeBase.Infrastructure.State
{
    public class MenuMainState : IState
    {
        private readonly SceneLoader _sceneLoader;
        private readonly GameStateMachine _stateMachine;
        private readonly IGameFactory _factory;
        private readonly IPersistentProgressService _progress;
        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowsService;
        private readonly LoadingCurtain _loadingCurtain;
        private readonly IPlayerCredentialsService _playerCredentials;
        private readonly IGoogleSignInService _googleSignIn;
        private readonly IServerService _serverService;
        private readonly IAudioService _audio;
        private readonly IAdsService _adsService;


        private MainWindow _mainWindow;

        public MenuMainState(GameStateMachine gameStateMachine, SceneLoader sceneLoader,
            IGameFactory factory, IPersistentProgressService progress, IUIFactory uiFactory,
            IWindowService windowsService, LoadingCurtain curtain, IPlayerCredentialsService playerCredentialsService,
            IGoogleSignInService googleSignIn, IServerService serverService, IAudioService audio, IAdsService ads)
        {
            _stateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _factory = factory;
            _progress = progress;
            _uiFactory = uiFactory;
            _windowsService = windowsService;
            _loadingCurtain = curtain;
   
            _playerCredentials = playerCredentialsService;
            _googleSignIn = googleSignIn;
            _serverService = serverService;
            _audio = audio;
            _adsService = ads;
        }

        public void Enter() =>
            _sceneLoader.Load(GameConstants.MenuSceneName, onLoaded: OpenMenu);


        private async void OpenMenu()
        {
            _factory.CreateAudioSource();
            await _uiFactory.CreateUIRoot();

            _mainWindow = ((MainWindow)await _windowsService.Open(WindowID.Main))
                .With(x => x.Construct(_progress.Progress, _audio, _playerCredentials, _adsService))
                .With(x => x.GetCompetitionsUI().Construct(_uiFactory, _serverService));
            
            SubscribeToMainWindowActionsAsync();

            if (_playerCredentials.Name.Length > 0 && _playerCredentials.Name != "null") {
                _mainWindow.SetLoginText(_playerCredentials.Name);
            } else {
                _googleSignIn.InitializePlatform(SignInLoginCallback);
            }

            await _uiFactory.SubscribeToEscape(EscapeButtonHandlerMainMenu);
            _loadingCurtain.Hide();
        }

        private void ExitApplication()
        {
            _uiFactory.UnSubscribeToEscape(EscapeButtonHandlerMainMenu);
            Application.Quit();
        }

        private void SubscribeToMainWindowActionsAsync()
        {
            _mainWindow.OnCompetitionChosenAction += () => {
                _mainWindow.OnCompetitionChosenAction -= OpenStagesState;
                _mainWindow.Kill();
                OpenStagesState();
            };
            _mainWindow.OnLoginButtonClickAction += () => {
                _googleSignIn.SignIn();
            };
            _mainWindow.OnStatisticButtonClickAction += () => {
                _mainWindow.OnStatisticButtonClickAction -= OpenStatisticState;
                _mainWindow.Kill();
                OpenStatisticState();
            };
            _mainWindow.OnAboutButtonClickAction += () => {
                _mainWindow.OnAboutButtonClickAction -= OpenAboutState;
                _mainWindow.Kill();
                OpenAboutState();
            };
        }

        private void OpenAboutState() =>
            _stateMachine.Enter<MenuAboutState>();

        private void OpenStatisticState() =>
            _stateMachine.Enter<MenuStatisticState>();

        private void OpenStagesState() =>
            _stateMachine.Enter<MenuStagesState>();

        private void SignInLoginCallback(bool success, string name, string id)
        {
            SetupPlayerCredentials(name, id);

            if (success) {
                _mainWindow.SetLoginText(_playerCredentials.Name);
                SendPlayerToServer();
            } else {
                _mainWindow.SetLoginText("sign-in failed. try again?");
            }
        }

        private void SetupPlayerCredentials(string name, string id) =>
            _playerCredentials.SetCredentials(name, id);

        private async void SendPlayerToServer()
        {
            await _serverService.PlayerCheckIn(ShowToastMessage);
        }

        private void ShowToastMessage(string message) =>
            AllServices.Container.Single<IAndroidToastMessageService>().ShowAndroidToastMessage(message);

        private void EscapeButtonHandlerMainMenu() => 
            _mainWindow.AdItem.ShowRewardedVideo(() => _uiFactory.ShowExitQuestion(ExitApplication));

        public void Exit()
        {
            _uiFactory.UnSubscribeToEscape(EscapeButtonHandlerMainMenu);
            _loadingCurtain.Show(false);
        }
    }
}
