using UnityEngine;

namespace CrazyLabs.Gameplay.Track
{
    public class Collectible : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _baseValue = 1;
        [SerializeField] private float _spinDegreesPerSecond = 120f;
        [SerializeField] private float _bobAmplitude = 0.12f;
        [SerializeField] private float _bobFrequency = 3f;

        private Vector3 _restLocalPosition;

        public int BaseValue => _baseValue;
        public bool IsCollected { get; private set; }


        public void Init()
        {
            IsCollected = false;
            _restLocalPosition = transform.localPosition;
        }

        public void Tick(float deltaTime, float time)
        {
            if (IsCollected)
            {
                return;
            }

            transform.Rotate(0f, _spinDegreesPerSecond * deltaTime, 0f, Space.World);
            var position = _restLocalPosition;
            position.y += Mathf.Sin(time * _bobFrequency + _restLocalPosition.z) * _bobAmplitude;
            transform.localPosition = position;
        }

        public void Collect()
        {
            IsCollected = true;
            gameObject.SetActive(false);
        }
    }
}
