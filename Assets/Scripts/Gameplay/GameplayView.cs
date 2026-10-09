using CrazyLabs.Gameplay.Feedback;
using CrazyLabs.Gameplay.Modules;
using Cysharp.Threading.Tasks;
using gSDK.MVC;
using UnityEngine;

namespace CrazyLabs.Gameplay
{
    public class GameplayView : BaseView<GameplayController>
    {
        [SerializeField] private Camera _cam;
        [SerializeField] private Light _sun;
        [SerializeField] private Vector3 _atmosphereEffectOffset = new(0f, 10f, 10f);
        [SerializeField] private TrackModule _trackModule;
        
        private AtmosphereModule _atmosphere;
        private CameraModule _camera;
        private FeedbackModule _feedback;
        
        
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
                _atmosphere?.Dispose();
                _atmosphere = new(_cam, _sun, _controller.Player.View.transform, _atmosphereEffectOffset, _controller.Level, transform);
                _trackModule.BuildLevel(_controller.Level, _controller.GroundTuning, _controller.SpawnTuning, _controller.SlingshotTuning);
            }

            if (_camera == null)
            {
                var playerTransform = _controller.Player.View.transform;

                _camera = new(_cam, playerTransform, _controller.CameraTuning);
                _feedback = new(gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>(), playerTransform, _controller.AudioTuning, _controller.EffectsTuning);
            }

            await RestartRun();
            await base.WillShow();
        }

        internal async UniTask RestartRun()
        {
            await _trackModule.RespawnProps(Random.Range(int.MinValue, int.MaxValue));
            _controller.StartRun(_trackModule.Profile);
            _camera.Snap();
        }

        void Update()
        {
            if (_controller?.Player == null)
            {
                return;
            }

            var player = _controller.Player;
            float deltaTime = Time.deltaTime;

            _controller.Tick(deltaTime);
            _atmosphere?.Tick();
            _trackModule.Tick(deltaTime, Time.time);
            _trackModule.TickSlingshot(player.IsAiming, player.PouchPosition);
            _camera?.Tick(deltaTime, Time.time, player.SpeedNormalized);
            _feedback?.Tick(player.Sled.IsSliding, player.SpeedNormalized);
        }

        private void DisposeModules()
        {
            _atmosphere?.Dispose();
            _camera?.Dispose();
            _feedback?.Dispose();
        }

        private void OnDestroy()
        {
            DisposeModules();
        }
    }
}