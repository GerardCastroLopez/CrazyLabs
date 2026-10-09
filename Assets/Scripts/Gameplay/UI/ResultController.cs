using System;
using CrazyLabs.Gameplay.Player;
using CrazyLabs.Player;
using CrazyLabs.Upgrades;
using Cysharp.Threading.Tasks;
using gSDK;
using gSDK.MVC;
using gSDK.UI;

namespace CrazyLabs.Gameplay.UI
{
    public class ResultController : UIViewController
    {
        private readonly Action _onRetry;
        private readonly Action _onForfeit;
        private readonly PlayerService _playerService;

        internal RunResult Result { get; private set; }
        internal UpgradesModule UpgradesModule => _playerService.UpgradesModule;
        internal Reactive<int> Currency => _playerService.Currency;


        public ResultController(Action onRetry, Action onForfeit, PlayerService playerService, UIService uiService) : base("UI/Prefabs/ResultView", 1, false, uiService)
        {
            _onRetry = onRetry;
            _onForfeit = onForfeit;
            _playerService = playerService;
        }

        internal void Show(RunResult result)
        {
            Result = result;
            ShowView(true).Forget();
        }

        internal void Retry()
        {
            _onRetry?.Invoke();
        }

        internal void Forfeit()
        {
            _onForfeit?.Invoke();
        }
    }
}
