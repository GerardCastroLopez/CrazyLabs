using System;
using CrazyLabs.Gameplay.Config;
using gSDK;
using gSDK.MVC;
using gSDK.UI;
using UnityEngine;

namespace CrazyLabs.Gameplay.UI
{
    public class GameplayUIController : UIViewController
    {
        private readonly GameplayController _gameplayController;
        private readonly Action _onPause;

        internal Reactive<int> Currency => _gameplayController.Currency;
        internal Reactive<float> Distance => _gameplayController.Distance;
        internal Reactive<float> Speed => _gameplayController.Speed;
        internal Reactive<bool> ShowTutorial => _gameplayController.ShowTutorial;
        internal PickupFeedbackTuningData PickupFeedback => _gameplayController.PickupFeedbackTuning;
        internal Camera Camera => _gameplayController.Camera;
        internal float TrackLength => _gameplayController.Level.TrackLength;
        
        
        public GameplayUIController(GameplayController gameplayController, Action onPause, UIService uiService) : base("UI/Prefabs/GameplayUIView", 0, false, uiService)
        {
            _onPause = onPause;
            _gameplayController = gameplayController;
        }

        internal void Pause()
        {
            _onPause?.Invoke();
        }
    }
}