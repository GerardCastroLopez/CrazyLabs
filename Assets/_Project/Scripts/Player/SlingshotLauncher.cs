using System;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>
    /// Handles the launch phase. The player drags down on screen (or holds Space) to pull the sled
    /// back, then releases to fire. Pull strength maps to launch speed.
    /// </summary>
    public sealed class SlingshotLauncher : MonoBehaviour
    {
        [SerializeField] SledMotor sled;
        [SerializeField] Transform leftTip;
        [SerializeField] Transform rightTip;
        [SerializeField] LineRenderer band;
        [SerializeField] float maxPullbackMeters = 2.5f;
        [SerializeField] float pouchHeight = 0.9f;
        [Tooltip("Vertical drag, as a fraction of screen height, that gives a full pull.")]
        [SerializeField, Range(0.05f, 0.6f)] float fullDragFraction = 0.2f;
        [SerializeField] float keyboardChargeSeconds = 1.1f;
        [Range(0.05f, 0.5f)]
        [SerializeField] float minimumPullToFire = 0.12f;

        bool active;
        bool pulling;
        float pull;
        float pressY;
        float keyCharge;

        /// <summary>Raised on release with the pull strength in [0, 1].</summary>
        public event Action<float> Fired;

        public float Pull => pull;

        public void Begin()
        {
            active = true;
            pulling = false;
            pull = 0f;
            keyCharge = 0f;
            Refresh();
        }

        public void End()
        {
            active = false;
            pulling = false;
            pull = 0f;
            Refresh();
        }

        /// <summary>Moves the slingshot posts to sit just ahead of the sled's start position.</summary>
        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            Refresh();
        }

        void Update()
        {
            if (!active) return;

            if (PointerInput.Pressed)
            {
                pulling = true;
                pressY = PointerInput.Position.y;
            }

            if (pulling && PointerInput.Held)
            {
                pull = Mathf.Clamp01((pressY - PointerInput.Position.y) / (Screen.height * fullDragFraction));
            }
            else if (Input.GetKey(KeyCode.Space))
            {
                keyCharge += Time.deltaTime / keyboardChargeSeconds;
                pull = Mathf.Clamp01(keyCharge);
                pulling = true;
            }
            else if (pulling)
            {
                Release();
                return;
            }

            Refresh();
        }

        void Release()
        {
            float strength = pull;
            pulling = false;
            keyCharge = 0f;

            if (strength < minimumPullToFire)
            {
                pull = 0f;
                Refresh();
                return;
            }

            Fired?.Invoke(strength);
        }

        void Refresh()
        {
            sled.SetPullback(pull * maxPullbackMeters);

            if (band == null || leftTip == null || rightTip == null) return;
            band.enabled = active;
            band.positionCount = 3;
            band.SetPosition(0, leftTip.position);
            band.SetPosition(1, sled.transform.position + Vector3.up * pouchHeight);
            band.SetPosition(2, rightTip.position);
        }
    }
}
