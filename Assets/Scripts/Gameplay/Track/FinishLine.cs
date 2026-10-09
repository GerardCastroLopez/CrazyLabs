using UnityEngine;

namespace CrazyLabs.Gameplay.Track
{
    public class FinishLine : MonoBehaviour
    {
        [SerializeField] private Transform _leftPost, _rightPost, _banner;
        [SerializeField] private BoxCollider _trigger;


        public void Place(Vector3 position, float width)
        {
            transform.position = position;

            SetX(_leftPost, -width * 0.5f);
            SetX(_rightPost, width * 0.5f);

            if (_banner)
            {
                var scale = _banner.localScale;
                scale.x = width;
                _banner.localScale = scale;
            }

            if (_trigger)
            {
                var size = _trigger.size;
                size.x = width;
                _trigger.size = size;
            }
        }

        private static void SetX(Transform target, float x)
        {
            if (target)
            {
                var position = target.localPosition;
                position.x = x;
                target.localPosition = position;
            }
        }
    }
}
