using CodeBase.Logic;
using CodeBase.Services.Audio;
using CodeBase.UI.Services.Factory;
using CodeBase.UI.Services.Windows;
using CodeBase.UI.Windows.UIElements;

namespace CodeBase.Infrastructure.State
{
    public class MenuAboutState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly LoadingCurtain _loadingCurtain;
        private readonly IUIFactory _uiFactory;
        private readonly IWindowService _windowService;
        private readonly IAudioService _audioService;

        private AboutWindow _aboutWindow;

        public MenuAboutState(GameStateMachine stateStateMachine,
            IUIFactory uiFactory, IWindowService windowService,
            LoadingCurtain loadingCurtain, IAudioService audioService)
        {
            _stateMachine = stateStateMachine;
            _loadingCurtain = loadingCurtain;
            _uiFactory = uiFactory;
            _windowService = windowService;
            _audioService = audioService;
        }

        public void Enter()
        {
            OpenAboutMenu();

        }

        private async void OpenAboutMenu()
        {
            _aboutWindow = (AboutWindow)await _windowService.Open(WindowID.AboutWindow);
            _aboutWindow.Construct(_audioService);
            _loadingCurtain.Hide();
            await _uiFactory.SubscribeToEscape(EscapeButtonHandlerStatisticMenu);
        }

        private void EscapeButtonHandlerStatisticMenu()
        {
            _audioService.Play(AudioClipName.ButtonClick);
            _aboutWindow.Kill();
            _stateMachine.Enter<MenuMainState>();
        }

        public void Exit()
        {
            _uiFactory.UnSubscribeToEscape(EscapeButtonHandlerStatisticMenu);
            _loadingCurtain.Show(false);
        }
    }

}
