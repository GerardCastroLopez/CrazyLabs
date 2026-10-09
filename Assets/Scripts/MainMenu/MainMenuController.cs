using System;
using CrazyLabs.Characters;
using CrazyLabs.Levels;
using CrazyLabs.Levels.Data;
using CrazyLabs.Player;
using CrazyLabs.Upgrades;
using gSDK;
using gSDK.MVC;
using gSDK.UI;

namespace CrazyLabs.MainMenu
{
    public class MainMenuController : UIViewController
    {
        internal Reactive<CharacterData> Character => _playerService.Character;
        internal UpgradesModule UpgradesModule => _playerService.UpgradesModule;
        internal Reactive<int> Currency => _playerService.Currency;
        internal Reactive<LevelData> Level => _levelService.Level;
        
        private readonly Action _onPlay;
        private readonly PlayerService _playerService;
        private readonly LevelService _levelService;
        
        
        public MainMenuController(Action onPlay, PlayerService playerService, LevelService levelService, UIService uiService) : base("UI/Prefabs/MainMenuView", 0, false, uiService)
        {
            _onPlay = onPlay;
            _playerService = playerService;
            _levelService = levelService;
        }

        internal void Play()
        {
            _onPlay?.Invoke();
        }

        internal void ChangeCharacter(int delta)
        {
            _playerService.ChangeCharacter(delta);
        }

        internal void ChangeLevel(int delta)
        {
            _levelService.ChangeLevel(delta);
        }
    }
}