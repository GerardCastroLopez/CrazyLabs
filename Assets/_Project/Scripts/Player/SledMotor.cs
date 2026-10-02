using System;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>
    /// Moves the player along the track profile.
    ///
    /// Steering is direct control of the sled's heading: input applies a turning force that spins the
    /// sled across the slope, and the heading stays where you leave it. Nothing recentres it, so you
    /// have to steer back to go straight. Speed is carried along the heading: gravity helps in
    /// proportion to how much the sled points downhill, friction and drag oppose motion, and turning
    /// scrubs a little speed.
    ///
    /// Motion is integrated manually (no physics forces); the kinematic rigidbody exists so trigger
    /// overlaps with obstacles and collectibles are reported.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SledMotor : MonoBehaviour
    {
        enum Phase { Parked, Sliding, Stopping }

        [SerializeField] SteeringInput steering;
        [SerializeField] float edgeMargin = 0.8f;

        Rigidbody body;
        TrackProfile profile;
        SledStats stats;
        Phase phase = Phase.Parked;

        float startZ;
        float z;
        float x;
        float speed;      // m/s along the heading
        float heading;    // radians from straight downhill, + = towards +X (right)
        float turnRate;   // rad/s
        float slideTime;
        bool stallRaised;

        public float Speed => speed;
        public float Distance => Mathf.Max(0f, z - startZ);
        public float SpeedNormalized => stats.MaxSpeed > 0f ? Mathf.Clamp01(speed / stats.MaxSpeed) : 0f;
        public float StartZ => startZ;
        public float HeadingDegrees => heading * Mathf.Rad2Deg;
        /// <summary>Current turn rate relative to the maximum, in [-1, 1]. Drives body lean.</summary>
        public float TurnNormalized => stats.MaxTurnRate > 0f ? Mathf.Clamp(turnRate / stats.MaxTurnRate, -1f, 1f) : 0f;
        public bool IsSliding => phase == Phase.Sliding;

        /// <summary>Raised once when speed collapses below the stall threshold.</summary>
        public event Action Stalled;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>Resets the sled to the start of the track, ready to be launched.</summary>
        public void Prepare(SledStats newStats, TrackProfile trackProfile, float startPosition)
        {
            stats = newStats;
            profile = trackProfile;
            startZ = startPosition;
            z = startPosition;
            x = 0f;
            speed = 0f;
            heading = 0f;
            turnRate = 0f;
            slideTime = 0f;
            stallRaised = false;
            phase = Phase.Parked;
            ApplyPose(teleport: true);
        }

        /// <summary>While aiming: slide the sled back up the slope as the slingshot is pulled.</summary>
        public void SetPullback(float meters)
        {
            if (phase != Phase.Parked || profile == null) return;
            z = startZ - meters;
            ApplyPose(teleport: true);
        }

        public void Launch(float launchSpeed)
        {
            speed = launchSpeed;
            heading = 0f;
            turnRate = 0f;
            slideTime = 0f;
            phase = Phase.Sliding;
        }

        /// <summary>Ends the run: the sled glides to a halt.</summary>
        public void Stop() => phase = Phase.Stopping;

        /// <summary>Instantly sheds speed (soft obstacle hit).</summary>
        public void ApplySlow(float factor) => speed *= Mathf.Clamp01(factor);

        void FixedUpdate()
        {
            if (phase == Phase.Parked || profile == null) return;

            float dt = Time.fixedDeltaTime;
            float pitch = profile.PitchAt(z);

            if (phase == Phase.Sliding)
            {
                Steer(dt);
                Accelerate(pitch, dt);
                slideTime += dt;

                if (!stallRaised && slideTime > stats.StallGraceSeconds && speed < stats.StallSpeed)
                {
                    stallRaised = true;
                    Stalled?.Invoke();
                }
            }
            else
            {
                speed = Mathf.MoveTowards(speed, 0f, stats.StopDeceleration * dt);
                turnRate = Mathf.MoveTowards(turnRate, 0f, 8f * dt);
            }

            x += speed * Mathf.Sin(heading) * dt;
            z = Mathf.Min(z + speed * Mathf.Cos(heading) * Mathf.Cos(pitch) * dt, profile.Length - 0.5f);
            ConstrainToTrack();
            ApplyPose(teleport: false);
        }

        /// <summary>Input is a turning force on the heading. Damping only stops the spin; it never recentres.</summary>
        void Steer(float dt)
        {
            float input = steering != null ? steering.Steer : 0f;
            turnRate += (input * stats.TurnAcceleration - turnRate * stats.TurnDamping) * dt;
            heading += turnRate * dt;

            if (Mathf.Abs(heading) > stats.MaxHeading)
            {
                heading = Mathf.Clamp(heading, -stats.MaxHeading, stats.MaxHeading);
                turnRate = 0f;
            }
        }

        void Accelerate(float pitch, float dt)
        {
            float downhill = stats.Gravity * Mathf.Sin(pitch) * Mathf.Cos(heading);   // less help the more you point across the slope
            float friction = stats.Gravity * stats.Friction * Mathf.Cos(pitch);
            float drag = stats.AirDrag * speed * speed;
            float carve = stats.CarveDrag * Mathf.Abs(turnRate) * speed;              // turning scrubs speed

            speed = Mathf.Clamp(speed + (downhill - friction - drag - carve) * dt, 0f, stats.MaxSpeed);
        }

        void ConstrainToTrack()
        {
            float limit = profile.HalfWidth - edgeMargin;
            if (Mathf.Abs(x) <= limit) return;

            x = Mathf.Clamp(x, -limit, limit);

            // Scraping the edge: stop pointing into the wall, but keep whatever heading points along or away from it.
            if (Mathf.Sign(heading) == Mathf.Sign(x))
            {
                heading = 0f;
                turnRate = 0f;
            }
        }

        void ApplyPose(bool teleport)
        {
            if (profile == null) return;

            var position = new Vector3(x, profile.HeightAt(z), z);
            var rotation = Quaternion.Euler(profile.PitchAt(z) * Mathf.Rad2Deg, heading * Mathf.Rad2Deg, 0f);

            if (teleport || body == null)
            {
                transform.SetPositionAndRotation(position, rotation);
                if (body != null) body.position = position;
            }
            else
            {
                body.MovePosition(position);
                body.MoveRotation(rotation);
            }
        }
    }
}
