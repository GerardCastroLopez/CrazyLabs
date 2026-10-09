using CrazyLabs.Upgrades;
using CrazyLabs.Upgrades.Data;
using DG.Tweening;
using gSDK;
using gSDK.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.MainMenu.UI
{
    public class UpgradesComponent : MonoBehaviour
    {
        [SerializeField] private TMP_Text _currencyTxt;
        [SerializeField] private UpgradeRowComponent _upgradePrefab;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private AudioClip _upgradeClip;
        [Header("Currency count animation")]
        [SerializeField] private float _currencyCountDelay = 0.4f;
        [SerializeField] private float _currencyCountDuration = 0.8f;
        [SerializeField] private Ease _currencyCountEase = Ease.OutCubic;

        private PooledScroll<UpgradeRowComponent> _scroll;
        private UpgradesModule _module;
        private AudioSource _audioSource;
        private Tween _currencyTween;


        void Awake()
        {
            _scroll = new(_upgradePrefab, _scrollRect, OnScrollItemUpdate);
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        public void Init(UpgradesModule upgrades, Reactive<int> currency)
        {
            _module = upgrades;
            _scroll.SetItemsCount(_module.Upgrades.Count, true);
            _scroll.FixContentIfFits(true);
            currency.AddListener(OnCurrencyChanged, true);
        }

        public void ForceCurrency(int count)
        {
            _currencyTxt.text = count.ToString();
        }

        public void AnimateCurrency(int from, int to)
        {
            _currencyTween?.Kill();

            if (from == to)
            {
                ForceCurrency(to);
                return;
            }

            int shown = from;
            ForceCurrency(from);
            _currencyTween = DOTween.To(() => shown, value => {

                shown = value;
                ForceCurrency(value);
            }, to, _currencyCountDuration).SetDelay(_currencyCountDelay).SetEase(_currencyCountEase).SetUpdate(true).SetTarget(this);
        }

        private void OnCurrencyChanged(int currency)
        {
            _currencyTween?.Kill();
            ForceCurrency(currency);
        }

        private void OnDestroy()
        {
            _currencyTween?.Kill();
        }

        private void OnScrollItemUpdate(UpgradeRowComponent row, int index)
        {
            var upgradeDef = _module.Upgrades[index];
            row.Init(upgradeDef, TryToUpgrade);
            RefreshRow(row, upgradeDef.Type);
        }

        private void TryToUpgrade(UpgradeType upgradeType)
        {
            if (_module.TryUpgrade(upgradeType))
            {
                _audioSource.PlayOneShot(_upgradeClip);
                _scroll.ForEach((row, _) => RefreshRow(row, row.UpgradeType));
            }
        }

        private void RefreshRow(UpgradeRowComponent upgradeRow, UpgradeType upgradeType)
        {
            upgradeRow.Refresh(_module.GetUpgradeLevel(upgradeType), _module.GetUpgradeCost(upgradeType), _module.CanUpgrade(upgradeType));
        }
    }
}