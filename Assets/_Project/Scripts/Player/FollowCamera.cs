using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>Chase camera that trails the sled and widens its field of view with speed.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] SledMotor sled;
        [SerializeField] Vector3 offset = new Vector3(0f, 4.2f, -8.5f);
        [SerializeField] Vector3 lookAhead = new Vector3(0f, 0.8f, 7f);
        [SerializeField] float followSmoothTime = 0.18f;
        [SerializeField] float baseFov = 55f;
        [SerializeField] float maxFovBoost = 14f;

        [Header("Shake")]
        [SerializeField] float maxShakeOffset = 0.6f;
        [SerializeField] float maxShakeRollDegrees = 4f;
        [SerializeField] float shakeFrequency = 28f;
        [Tooltip("Trauma lost per second. Lower = shakes last longer.")]
        [SerializeField] float traumaDecay = 1.4f;

        Camera cam;
        Vector3 velocity;
        Vector3 smoothedPosition; // follow position before shake is applied
        float trauma;
        float shakeSeed;

        void Awake()
        {
            cam = GetComponent<Camera>();
            shakeSeed = Random.value * 100f;
        }

        /// <summary>Adds screen shake. Strength is in [0, 1]; effects stack and decay over time.</summary>
        public void Shake(float strength) => trauma = Mathf.Clamp01(trauma + strength);

        void LateUpdate()
        {
            Vector3 desired = DesiredPosition();
            smoothedPosition = Vector3.SmoothDamp(smoothedPosition, desired, ref velocity, followSmoothTime);
            transform.position = smoothedPosition;
            transform.rotation = Quaternion.LookRotation(target.position + lookAhead - smoothedPosition);
            ApplyShake();
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + maxFovBoost * sled.SpeedNormalized, Time.deltaTime * 3f);
        }

        void ApplyShake()
        {
            if (trauma <= 0f) return;

            // Squared so small hits are subtle and big ones violent; Perlin noise keeps it smooth, not jittery.
            float amount = trauma * trauma;
            float t = Time.time * shakeFrequency;
            var offset = new Vector3(Noise(t, 0f), Noise(t, 1f), 0f) * (maxShakeOffset * amount);
            transform.position += transform.rotation * offset;
            transform.rotation *= Quaternion.Euler(0f, 0f, Noise(t, 2f) * maxShakeRollDegrees * amount);

            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);
        }

        float Noise(float time, float channel) => Mathf.PerlinNoise(shakeSeed + channel * 17.3f, time) * 2f - 1f;

        public void Snap()
        {
            trauma = 0f;
            velocity = Vector3.zero;
            smoothedPosition = DesiredPosition();
            transform.position = smoothedPosition;
            transform.rotation = Quaternion.LookRotation(target.position + lookAhead - smoothedPosition);
        }

        Vector3 DesiredPosition()
        {
            // The camera ignores the sled's lateral position so steering reads clearly on screen.
            var anchor = new Vector3(target.position.x * 0.5f, target.position.y, target.position.z);
            return anchor + offset;
        }
    }
}
