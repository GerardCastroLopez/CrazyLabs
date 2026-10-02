using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>Start screen: title, upgrades and the Launch button.</summary>
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] Button launchButton;
        [SerializeField] UpgradePanelView upgrades;
        [SerializeField] SelectorView characterSelector;
        [SerializeField] SelectorView levelSelector;

        public UpgradePanelView Upgrades => upgrades;
        public SelectorView CharacterSelector => characterSelector;
        public SelectorView LevelSelector => levelSelector;
        public event Action LaunchClicked;

        void Awake() => launchButton.onClick.AddListener(() => LaunchClicked?.Invoke());

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
