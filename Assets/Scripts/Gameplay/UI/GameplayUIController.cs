using gSDK;
using gSDK.MVC;
using gSDK.UI;

namespace CrazyLabs.Gameplay.UI
{
    public class GameplayUIController : UIViewController
    {
        private readonly GameplayController _gameplayController;

        internal Reactive<int> Currency => _gameplayController.Currency;
        internal Reactive<float> Distance => _gameplayController.Distance;
        internal Reactive<float> Speed => _gameplayController.Speed;
        internal Reactive<bool> ShowTutorial => _gameplayController.ShowTutorial;
        internal float TrackLength => _gameplayController.Level.TrackLength;
        
        
        public GameplayUIController(GameplayController gameplayController, UIService uiService) : base("Prefabs/GameplayUIView", 0, false, uiService)
        {
            _gameplayController = gameplayController;
        }
    }
}