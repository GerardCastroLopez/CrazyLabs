using CrazyLabs.Gameplay.Events;
using CrazyLabs.Gameplay.Feedback;
using CrazyLabs.Gameplay.Modules;
using CrazyLabs.Gameplay.Track;
using Cysharp.Threading.Tasks;
using gSDK.EventSystem;
using gSDK.MVC;
using UnityEngine;

namespace CrazyLabs.Gameplay
{
    public class GameplayView : BaseView<GameplayController>, IEventHandler<ObstacleHitEvent>
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Camera _cam;
        [SerializeField] private Light _sun;
        [SerializeField] private Vector3 _atmosphereEffectOffset = new(0f, 10f, 10f);
        [SerializeField] private TrackModule _trackModule;
        [SerializeField] private SlingshotVisual _slingshot;
        
        private Transform _playerTransform;
        private AtmosphereModule _atmosphere;
        private CameraModule _camera;
        private AudioModule _audio;
        private ParticleModule _particles;
        
        
        protected override void OnBind()
        {
            base.OnBind();

            EventDispatcher.Register(this);
        }

        protected override UniTask InternalShow(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override async UniTask WillShow()
        {
            if (_controller.MustLoadLevel)
            {
                _playerTransform = _controller.Player.View.transform;
                _atmosphere = new(_cam, _sun, _playerTransform, _atmosphereEffectOffset, _controller.Level, transform);
                _trackModule.BuildLevel(_controller.Level);
                _camera = new(_cam, _playerTransform, _controller.CameraTuning);
                CreateFeedback();
                PlaceSlingshot();
            }

            await _trackModule.RespawnProps(Random.Range(int.MinValue, int.MaxValue));
            _controller.StartRun(_trackModule.Profile);
            _camera.Snap();

            await base.WillShow();
        }

        void Update()
        {
            if (_controller?.Player == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            _controller.Tick(deltaTime);
            _atmosphere?.Tick();
            _trackModule.Tick(deltaTime, Time.time);
            _camera?.Tick(deltaTime, Time.time, _controller.Player.SpeedNormalized);
            TickFeedback();
        }

        public void Handle(ObstacleHitEvent evt)
        {
            var tuning = _controller.CameraTuning;
            _camera?.Shake(evt.Obstacle.Kind == ObstacleKind.Crash ? tuning.CrashShake : tuning.SlowHitShake);
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
            _audio?.Dispose();
            _particles?.Dispose();
        }

        private void CreateFeedback()
        {
            var effects = _controller.EffectsTuning;

            _audio = new(_audioSource ? _audioSource : gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>(), _controller.AudioTuning);
            _particles = new(_playerTransform, effects);
        }

        private void PlaceSlingshot()
        {
            var profile = _trackModule.Profile;
            float z = _controller.SlingshotTuning.StartZ + _controller.SlingshotTuning.PostForwardOffset;
            _slingshot.Place(new Vector3(0f, profile.HeightAt(z), z), Quaternion.Euler(profile.PitchAt(z) * Mathf.Rad2Deg, 0f, 0f));
        }

        private void TickFeedback()
        {
            var player = _controller.Player;

            _audio?.Tick(player.Sled.IsSliding, player.SpeedNormalized);
            _particles?.Tick(player.Sled.IsSliding, player.SpeedNormalized);
            _slingshot?.Tick(player.IsAiming, player.PouchPosition);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
