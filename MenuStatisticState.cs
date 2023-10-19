using CodeBase.Infrastructure.Data;
using CodeBase.Infrastructure.Data.DataTypes;
using CodeBase.Logic;
using CodeBase.Services.AssetManagement;
using CodeBase.Services.Audio;
using CodeBase.Services.PersistentProgress;
using CodeBase.Services.Server;
using CodeBase.Services.StaticData;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows;
using UnityEngine;

namespace CodeBase.Infrastructure.State
{
    public class MenuStatisticState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly LoadingCurtain _loadingCurtain;
        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowService;
        private readonly IPersistentProgressService _progress;
        private readonly IAudioService _audioService;
        private readonly IServerService _serverService;
        private readonly IStaticDataService _staticDataService;
        private readonly IAdsService _adsService;

        private StatisticWindow _statisticWindow;
        private bool _isGlobalOpened;
        private bool _leaderboardShown;

        public MenuStatisticState(
            GameStateMachine stateStateMachine,
            IPersistentProgressService persistentProgressService,
            IUIFactory uiFactory,
            IWindowService windowService,
            LoadingCurtain loadingCurtain,
            IAudioService audioService,
            IServerService serverService,
            IStaticDataService staticDataService,
            IAdsService adsService)
        {
            _stateMachine = stateStateMachine;
            _loadingCurtain = loadingCurtain;
            _uiFactory = uiFactory;
            _windowService = windowService;
            _progress = persistentProgressService;
            _audioService = audioService;
            _serverService = serverService;
            _staticDataService = staticDataService;
            _adsService = adsService;
        }

        public void Enter()
        {
            OpenStatisticMenu();
            _leaderboardShown = false;

        }

        private async void OpenStatisticMenu()
        {
            _statisticWindow = (StatisticWindow)await _windowService.Open(WindowID.Statistic);
            _statisticWindow.Construct(
                _progress.Progress,
                _audioService,
                _loadingCurtain,
                _uiFactory,
                _staticDataService,
                _adsService);
            _statisticWindow.OnGlobalPanelOpenAction += GlobalOpened;
            _statisticWindow.OnGlobalPanelCloseAction += GlobalClosed;
            _statisticWindow.OnGlobalStageStatisticAction += ToGlobalStage;
            _statisticWindow.OnGlobalCompetitionStatisticAction += ToGlobalCompetition;

            await _uiFactory.SubscribeToEscape(EscapeButtonHandlerStatisticMenu);

            CreateCleanProgressButton();
        }

        private async void CreateCleanProgressButton()
        {
            var cleanGo = await _uiFactory.CreateUiElementAsync(
                AssetAddress.CleanProgressButton,
                _statisticWindow.transform);
            cleanGo.GetComponent<CleanProgressButton>().Construct(_uiFactory, _audioService, CleanProgressEvent);
        }

        private void GlobalClosed() => _isGlobalOpened = false;

        private void GlobalOpened() => _isGlobalOpened = true;


        private async void ToGlobalCompetition(CompetitionType type)
        {
            if(!_leaderboardShown)
            {
                _loadingCurtain.Show(false);
                _leaderboardShown = true;
                await _serverService.GetGlobalCompetitionStatisticAsync(type, _statisticWindow.ConstructGlobalPanel);
            } else
            {
                _leaderboardShown = false;
                _statisticWindow.AdsItem.ShowRewardedVideo(() => ToGlobalCompetition(type));
            }
        }

        private async void ToGlobalStage(StageResults stageResults)
        {
            if(!_leaderboardShown)
            {
                _leaderboardShown = true;
                _loadingCurtain.Show(false);
                await _serverService.GetGlobalStageStatistic(stageResults, _statisticWindow.ConstructGlobalPanel);
            } else
            {
                _leaderboardShown = false;
                _statisticWindow.AdsItem.ShowRewardedVideo(() => ToGlobalStage(stageResults));
            }
        }

        private void EscapeButtonHandlerStatisticMenu()
        {
            _audioService.Play(AudioClipName.ButtonClick);
            //Debug.Log("_isGlobalOpened = " + _isGlobalOpened);
            if(_isGlobalOpened)
            {
                _isGlobalOpened = false;
                _statisticWindow.HideGlobal();
            } else
            {
                _statisticWindow.Kill();
                _stateMachine.Enter<MenuMainState>();
            }
        }

        private void CleanProgressEvent()
        {
            _serverService.CleanProgress();//сначала чистим прогресс на сервере с ID игрока
            _progress.Progress.CleanProgress();//а потом локальный прогресс вместе с ID игрока

            _statisticWindow.Kill();
            _stateMachine.Enter<LoadProgressState>();
        }

        public void Exit()
        {
            _uiFactory.UnSubscribeToEscape(EscapeButtonHandlerStatisticMenu);
            _loadingCurtain.Show(false);
        }
    }
}
