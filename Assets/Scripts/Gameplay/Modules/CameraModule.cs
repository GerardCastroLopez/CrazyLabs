using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Events;
using CrazyLabs.Gameplay.Track;
using gSDK.EventSystem;
using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class CameraModule : IEventHandler<ObstacleHitEvent>
    {
        private const float kShakeSeedRange = 100f;
        private const float kNoiseChannelSpacing = 17.3f;
        private const float kNoiseChannelX = 0f;
        private const float kNoiseChannelY = 1f;
        private const float kNoiseChannelRoll = 2f;

        private readonly Camera _cam;
        private readonly Transform _target;
        private readonly CameraTuningData _tuning;
        private readonly float _shakeSeed;

        private Vector3 _smoothedPosition;
        private Vector3 _velocity;
        private float _trauma;
        private float _pitch, _pitchVelocity;


        public CameraModule(Camera cam, Transform target, CameraTuningData tuning)
        {
            _cam = cam;
            _target = target;
            _tuning = tuning;
            _shakeSeed = Random.value * kShakeSeedRange;

            EventDispatcher.Register(this);
        }

        public void Handle(ObstacleHitEvent evt)
        {
            Shake(evt.Obstacle.Kind == ObstacleKind.Crash ? _tuning.CrashShake : _tuning.SlowHitShake);
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
        }

        public void Shake(float strength)
        {
            _trauma = Mathf.Clamp01(_trauma + strength);
        }

        public void Snap()
        {
            _trauma = 0f;
            _velocity = Vector3.zero;
            _pitchVelocity = 0f;
            _pitch = GetTargetPitch();
            _smoothedPosition = GetDesiredPosition();
            ApplyFollow();
        }

        public void Tick(float deltaTime, float time, float speedNormalized)
        {
            _pitch = Mathf.SmoothDampAngle(_pitch, GetTargetPitch(), ref _pitchVelocity, _tuning.FollowSmoothTime, Mathf.Infinity, deltaTime);
            _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, GetDesiredPosition(), ref _velocity, _tuning.FollowSmoothTime, Mathf.Infinity, deltaTime);
            ApplyFollow();
            ApplyShake(deltaTime, time);

            float fov = _tuning.BaseFov + _tuning.MaxFovBoost * speedNormalized;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, deltaTime * _tuning.FovLerpSpeed);
        }

        private void ApplyFollow()
        {
            var tr = _cam.transform;
            tr.position = _smoothedPosition;
            tr.rotation = Quaternion.LookRotation(_target.position + PitchRotation * _tuning.LookAhead - _smoothedPosition);
        }

        private void ApplyShake(float deltaTime, float time)
        {
            if (_trauma <= 0f)
            {
                return;
            }

            float amount = _trauma * _trauma;
            float t = time * _tuning.ShakeFrequency;
            var offset = new Vector3(Noise(t, kNoiseChannelX), Noise(t, kNoiseChannelY), 0f) * (_tuning.MaxShakeOffset * amount);

            var tr = _cam.transform;
            tr.position += tr.rotation * offset;
            tr.rotation *= Quaternion.Euler(0f, 0f, Noise(t, kNoiseChannelRoll) * _tuning.MaxShakeRollDegrees * amount);

            _trauma = Mathf.Max(0f, _trauma - _tuning.TraumaDecay * deltaTime);
        }

        private float Noise(float time, float channel)
        {
            return Mathf.PerlinNoise(_shakeSeed + channel * kNoiseChannelSpacing, time) * 2f - 1f;
        }

        private Vector3 GetDesiredPosition()
        {
            var anchor = new Vector3(_target.position.x * _tuning.LateralFollowFactor, _target.position.y, _target.position.z);
            return anchor + PitchRotation * _tuning.Offset;
        }

        private Quaternion PitchRotation => Quaternion.Euler(_pitch, 0f, 0f);

        private float GetTargetPitch()
        {
            return Mathf.DeltaAngle(0f, _target.eulerAngles.x) * _tuning.PitchFollowFactor;
        }
    }
}
