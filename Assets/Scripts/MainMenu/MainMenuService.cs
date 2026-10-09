using CrazyLabs.Audio;
using CrazyLabs.Gameplay;
using CrazyLabs.Levels;
using CrazyLabs.Player;
using Cysharp.Threading.Tasks;
using gSDK.Services;
using gSDK.UI;

namespace CrazyLabs.MainMenu
{
    public class MainMenuService : BaseService
    {
        private MainMenuController _controller;
        private GameplayService _gameplayService;
        private MusicService _musicService;
        
        
        public MainMenuService() : base(false)
        {
        }

        protected override void Init()
        {
            base.Init();

            _gameplayService = _locator.GetService<GameplayService>();
            _musicService = _locator.GetService<MusicService>();
            var uiService = _locator.GetService<UIService>();
            _controller = new(Play, _locator.GetService<PlayerService>(), _locator.GetService<LevelService>(), uiService);

            ShowAsync(uiService).Forget();
        }

        private async UniTask ShowAsync(UIService uiService)
        {
            await _controller.CacheView();
            
            uiService.ToggleLoading(false, true);
            _musicService.PlayMenu();
            _controller.ShowView(true).Forget();
        }

        public void Show()
        {
            _musicService.PlayMenu();
            _controller.ShowView(true).Forget();
        }

        private void Play()
        {
            _gameplayService.StartGame(() => _controller.HideView(false).Forget());
        }
    }
}