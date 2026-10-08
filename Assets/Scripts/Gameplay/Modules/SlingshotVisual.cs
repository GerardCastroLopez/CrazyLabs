using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class SlingshotVisual : MonoBehaviour
    {
        [SerializeField] private LineRenderer _band;
        [SerializeField] private Transform _leftTip, _rightTip;

        private const int kLeftPoint = 0;
        private const int kPouchPoint = 1;
        private const int kRightPoint = 2;


        public void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Tick(bool aiming, Vector3 pouchPosition)
        {
            _band.enabled = aiming;

            if (!aiming)
            {
                return;
            }

            _band.SetPosition(kLeftPoint, _leftTip.position);
            _band.SetPosition(kPouchPoint, pouchPosition);
            _band.SetPosition(kRightPoint, _rightTip.position);
        }
    }
}
