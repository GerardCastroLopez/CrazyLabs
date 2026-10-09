using System;
using CrazyLabs.Audio;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Player;
using CrazyLabs.Gameplay.UI;
using CrazyLabs.Levels;
using CrazyLabs.MainMenu;
using CrazyLabs.Player;
using Cysharp.Threading.Tasks;
using gSDK.MVC;
using gSDK.Services;
using gSDK.UI;

namespace CrazyLabs.Gameplay
{
    public class GameplayService : BaseService
    {
        private GameplayController _gameplayController;
        private GameplayUIController _uiController;
        private PauseController _pauseController;
        private ResultController _resultController;
        private MainMenuService _mainMenuService;
        private MusicService _musicService;
        private UIService _uiService;
        private int _runVersion;
        
        
        public GameplayService() : base(false)
        {
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();

            _uiService = _locator.GetService<UIService>();
            var playerService = _locator.GetService<PlayerService>();

            _gameplayController = new(_locator.GetConfig<GameplayConfigSO>(), playerService, _locator.GetService<LevelService>(), OnRunEnded);
            _uiController = new(_gameplayController, Pause, _uiService);
            _pauseController = new(Resume, Restart, Home, _locator.GetService<PopupService>());
            _resultController = new(Restart, Home, playerService, _uiService);
        }

        protected override void Init()
        {
            base.Init();

            _mainMenuService = _locator.GetService<MainMenuService>();
            _musicService = _locator.GetService<MusicService>();
        }

        public void StartGame(Action onLoadingShown)
        {
            AsyncStartGame(onLoadingShown).Forget();
        }

        private async UniTaskVoid AsyncStartGame(Action onLoadingShown)
        {
            _runVersion++;
            await UniTask.WhenAll(_uiService.AsyncToggleLoading(true, true), _gameplayController.CacheView(), _uiController.CacheView(), _pauseController.CacheView(), _resultController.CacheView());
            _musicService.Play(_gameplayController.Level.Music);
            onLoadingShown?.Invoke();
            await UniTask.WhenAll(_uiService.AsyncToggleLoading(false, true), _gameplayController.ShowView(false));
            await _uiController.ShowView(true);
        }

        private void Pause()
        {
            if (!_gameplayController.CanPause || _pauseController.ViewState.Value != eViewState.Hidden)
            {
                return;
            }

            _gameplayController.SetPaused(true);
            _pauseController.ShowView(true).Forget();
        }

        private void Resume()
        {
            _gameplayController.SetPaused(false);
            _pauseController.HideView(true).Forget();
        }

        private void Restart()
        {
            _runVersion++;
            _gameplayController.SetPaused(false);
            _pauseController.HideView(true).Forget();
            _resultController.HideView(true).Forget();
            _gameplayController.RestartRun().Forget();
        }

        private void Home()
        {
            _runVersion++;
            _pauseController.HideView(false).Forget();
            _resultController.HideView(false).Forget();
            HomeAsync().Forget();
        }

        private async UniTaskVoid HomeAsync()
        {
            await UniTask.WhenAll(_uiController.HideView(false), _gameplayController.HideView(false));
            _mainMenuService.Show();
        }

        private void OnRunEnded(RunResult result)
        {
            ShowResultAfterDelay(result, _runVersion).Forget();
        }

        private async UniTaskVoid ShowResultAfterDelay(RunResult result, int version)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_gameplayController.FlowTuning.ResultPopupDelay));

            if (version == _runVersion)
            {
                _resultController.Show(result);
            }
        }
    }
}
