using System;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.UI;
using CrazyLabs.Levels;
using CrazyLabs.Player;
using Cysharp.Threading.Tasks;
using gSDK.Services;
using gSDK.UI;

namespace CrazyLabs.Gameplay
{
    public class GameplayService : BaseService
    {
        private GameplayController _gameplayController;
        private GameplayUIController _uiController;
        private UIService _uiService;
        
        
        public GameplayService() : base(false)
        {
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();

            _uiService = _locator.GetService<UIService>();

            _gameplayController = new(_locator.GetConfig<GameplayConfigSO>(), _locator.GetService<PlayerService>(), _locator.GetService<LevelService>());
            _uiController = new(_gameplayController, _uiService);
        }

        public void StartGame(Action onLoadingShown)
        {
            AsyncStartGame(onLoadingShown).Forget();
        }

        private async UniTaskVoid AsyncStartGame(Action onLoadingShown)
        {
            await UniTask.WhenAll(_uiService.AsyncToggleLoading(true, true), _gameplayController.CacheView(), _uiController.CacheView());
            onLoadingShown?.Invoke();
            await UniTask.WhenAll(_uiService.AsyncToggleLoading(false, true), _gameplayController.ShowView(false));
            await _uiController.ShowView(true);
        }
    }
}