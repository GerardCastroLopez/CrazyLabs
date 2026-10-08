using CrazyLabs.Gameplay.Track;
using UnityEngine;

namespace CrazyLabs.Gameplay.Sled
{
    public class SledModule
    {
        private enum ePhase
        {
            Parked,
            Sliding,
            Stopping,
        }

        private const float kEdgeMargin = 0.8f;
        private const float kStoppingTurnDamping = 8f;
        private const float kRunoutMargin = 0.5f;

        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public float Speed => _speed;
        public float SpeedNormalized => _stats.MaxSpeed > 0f ? Mathf.Clamp01(_speed / _stats.MaxSpeed) : 0f;
        public float Distance => Mathf.Max(0f, _z - _startZ);
        public float TurnNormalized => _stats.MaxTurnRate > 0f ? Mathf.Clamp(_turnRate / _stats.MaxTurnRate, -1f, 1f) : 0f;

        public bool IsSliding => _phase == ePhase.Sliding;
        public bool HasStalled { get; private set; }

        private SledStats _stats;
        private TrackProfile _profile;
        private ePhase _phase = ePhase.Parked;

        private float _startZ, _x, _z, _speed, _heading, _turnRate, _slideTime;


        public void Prepare(SledStats stats, TrackProfile profile, float startZ)
        {
            _stats = stats;
            _profile = profile;
            _startZ = startZ;
            _z = startZ;
            _x = 0f;
            _speed = 0f;
            _heading = 0f;
            _turnRate = 0f;
            _slideTime = 0f;
            HasStalled = false;
            _phase = ePhase.Parked;

            UpdatePose();
        }

        public void SetPullback(float meters)
        {
            if (_phase != ePhase.Parked || _profile == null)
            {
                return;
            }

            _z = _startZ - meters;
            UpdatePose();
        }

        public void Launch(float launchSpeed)
        {
            _speed = launchSpeed;
            _heading = _turnRate = _slideTime = 0f;
            _phase = ePhase.Sliding;
        }

        public void Stop()
        {
            _phase = ePhase.Stopping;
        }

        public void ApplySlow(float factor)
        {
            _speed *= Mathf.Clamp01(factor);
        }

        public void Tick(float deltaTime, float steer)
        {
            if (_phase == ePhase.Parked || _profile == null)
            {
                return;
            }

            float pitch = _profile.PitchAt(_z);

            if (_phase == ePhase.Sliding)
            {
                Steer(deltaTime, steer);
                Accelerate(pitch, deltaTime);
                _slideTime += deltaTime;

                if (_slideTime > _stats.StallGraceSeconds && _speed < _stats.StallSpeed)
                {
                    HasStalled = true;
                }
            }
            else
            {
                _speed = Mathf.MoveTowards(_speed, 0f, _stats.StopDeceleration * deltaTime);
                _turnRate = Mathf.MoveTowards(_turnRate, 0f, kStoppingTurnDamping * deltaTime);
            }

            _x += _speed * Mathf.Sin(_heading) * deltaTime;
            _z = Mathf.Min(_z + _speed * Mathf.Cos(_heading) * Mathf.Cos(pitch) * deltaTime, _profile.Length - kRunoutMargin);

            ConstrainToTrack();
            UpdatePose();
        }

        private void Steer(float deltaTime, float input)
        {
            _turnRate += (input * _stats.TurnAcceleration - _turnRate * _stats.TurnDamping) * deltaTime;
            _heading += _turnRate * deltaTime;

            if (Mathf.Abs(_heading) > _stats.MaxHeading)
            {
                _heading = Mathf.Clamp(_heading, -_stats.MaxHeading, _stats.MaxHeading);
                _turnRate = 0f;
            }
        }

        private void Accelerate(float pitch, float deltaTime)
        {
            float downhill = _stats.Gravity * Mathf.Sin(pitch) * Mathf.Cos(_heading);
            float friction = _stats.Gravity * _stats.Friction * Mathf.Cos(pitch);
            float drag = _stats.AirDrag * _speed * _speed;
            float carve = _stats.CarveDrag * Mathf.Abs(_turnRate) * _speed;

            _speed = Mathf.Clamp(_speed + (downhill - friction - drag - carve) * deltaTime, 0f, _stats.MaxSpeed);
        }

        private void ConstrainToTrack()
        {
            float limit = _profile.HalfWidth - kEdgeMargin;
            if (Mathf.Abs(_x) <= limit)
            {
                return;
            }

            _x = Mathf.Clamp(_x, -limit, limit);

            if (Mathf.Sign(_heading) == Mathf.Sign(_x))
            {
                _heading = 0f;
                _turnRate = 0f;
            }
        }

        private void UpdatePose()
        {
            Position = new Vector3(_x, _profile.HeightAt(_z), _z);
            Rotation = Quaternion.Euler(_profile.PitchAt(_z) * Mathf.Rad2Deg, _heading * Mathf.Rad2Deg, 0f);
        }
    }
}
