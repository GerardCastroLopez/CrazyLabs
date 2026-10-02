using UnityEngine;

namespace CrazyLabs.Track
{
    /// <summary>A pickup (croissant) that is worth a number of coins.</summary>
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField, Min(1)] int baseValue = 1;
        [SerializeField] float spinDegreesPerSecond = 120f;
        [SerializeField] float bobAmplitude = 0.12f;

        Vector3 restLocalPosition;

        public int BaseValue => baseValue;

        void Start() => restLocalPosition = transform.localPosition;

        void Update()
        {
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            var position = restLocalPosition;
            position.y += Mathf.Sin(Time.time * 3f + restLocalPosition.z) * bobAmplitude;
            transform.localPosition = position;
        }

        public void Collect() => gameObject.SetActive(false);
    }
}
