using CrazyLabs.Characters;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Controls;
using CrazyLabs.Gameplay.Player;
using CrazyLabs.Gameplay.Track;
using CrazyLabs.Levels;
using CrazyLabs.Levels.Data;
using CrazyLabs.Player;
using Cysharp.Threading.Tasks;
using gSDK;
using gSDK.MVC;

namespace CrazyLabs.Gameplay
{
    public class GameplayController : ViewController
    {
        private readonly GameplayConfigSO _config;
        private readonly PlayerService _playerService;
        private readonly LevelService _levelService;
        
        private PlayerController _player;
        
        internal CharacterData Character => _playerService.Character.Value;
        internal LevelData Level { get; private set; }
        internal bool MustLoadLevel { get; private set; }
        internal PlayerController Player => _player;
        internal CameraTuningData CameraTuning => _config.CameraRig;
        internal AudioTuningData AudioTuning => _config.Audio;
        internal EffectsTuningData EffectsTuning => _config.Effects;
        internal SlingshotTuningData SlingshotTuning => _config.Slingshot;
        
        internal readonly Reactive<int> Currency = new(0);
        internal readonly Reactive<float> Distance = new(0f);
        internal readonly Reactive<float> Speed = new(0f);
        internal readonly Reactive<bool> ShowTutorial = new(false);
        internal readonly Reactive<eInputDevice> ActiveDevice = new(eInputDevice.Pointer);


        
        public GameplayController(GameplayConfigSO config, PlayerService playerService, LevelService levelService) : base("Prefabs/GameplayView", false)
        {
            _config = config;
            _playerService = playerService;
            _levelService = levelService;
        }

        public override async UniTask CacheView()
        {
            EnsureLevel();
            await base.CacheView();

            EnsurePlayer();
            await _player.CacheView();
        }

        public override async UniTask ShowView(bool animate, bool force = false)
        {
            await base.ShowView(animate, force);
            await _player.ShowView(animate, force);
        }

        public override void Destroy()
        {
            DestroyPlayer();
            base.Destroy();
        }

        internal void Tick(float deltaTime)
        {
            _player?.Tick(deltaTime);
        }

        internal void StartRun(TrackProfile profile)
        {
            Currency.Value = 0;
            _player.StartRun(profile);
        }

        private void EnsureLevel()
        {
            MustLoadLevel = Level == null || !View;
            
            var level = _levelService.Level.Value;
            
            if (Level != level && Level != null)
            {
                Destroy();
                MustLoadLevel = true;
            }

            Level = level;

            Currency.Value = 0;
            Distance.Value = 0f;
            Speed.Value = 0f;
            ShowTutorial.Value = true;
        }
        
        private void EnsurePlayer()
        {
            if (_player != null && _player.View)
            {
                return;
            }

            DestroyPlayer();

            _player = new(this, _config, _playerService);
            _player.SetParent(View.transform);
        }

        private void DestroyPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _player.Destroy();
            _player = null;
        }
    }
}
