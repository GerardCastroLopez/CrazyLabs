using CrazyLabs.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>In-run overlay: collected coins, distance, speed, and the aiming hint.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] Text coinsText;
        [SerializeField] Text distanceText;
        [SerializeField] Text speedText;
        [SerializeField] GameObject aimHint;

        void Update()
        {
            bool aiming = flow.State == GameState.Aiming;
            aimHint.SetActive(aiming);

            coinsText.text = flow.Session.Coins.ToString();
            distanceText.text = $"{flow.Sled.Distance:0} / {flow.TrackLength:0} m";
            speedText.text = $"{flow.Sled.Speed * 3.6f:0} km/h";
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
