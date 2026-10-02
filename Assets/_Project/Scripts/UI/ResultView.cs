using System;
using CrazyLabs.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>End-of-run screen: outcome, stats, upgrades for the next run, and Retry.</summary>
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] Text titleText;
        [SerializeField] Text detailText;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;
        [SerializeField] UpgradePanelView upgrades;

        public UpgradePanelView Upgrades => upgrades;
        public event Action RetryClicked;
        public event Action MenuClicked;

        void Awake()
        {
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        public void Show(RunResult result)
        {
            titleText.text = result.Outcome switch
            {
                RunOutcome.ReachedFinish => "You made it!",
                RunOutcome.Crashed => "Crashed!",
                _ => "Out of momentum",
            };
            detailText.text = $"Distance  {result.DistanceMeters:0} m  ({result.Progress01 * 100f:0}%)\nCroissants  +{result.CoinsCollected}";
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
