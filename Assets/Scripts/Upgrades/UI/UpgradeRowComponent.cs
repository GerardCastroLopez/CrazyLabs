using System;
using CrazyLabs.Upgrades.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.MainMenu.UI
{
    public sealed class UpgradeRowComponent : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameTxt, _levelTxt, _costTxt;
        [SerializeField] private Button _upgradeBtn;

        [Header("Purchase feedback")]
        [SerializeField] private Graphic _flashGraphic;
        [SerializeField] private Color _flashColor = new(1f, 0.85f, 0.3f, 0.6f);
        [SerializeField] private float _flashDuration = 0.4f;
        [SerializeField] private float _punchScale = 0.12f;
        [SerializeField] private float _punchDuration = 0.35f;

        [Header("States")]
        [SerializeField] private Color _maxColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color _unaffordableColor = new(1f, 0.45f, 0.4f, 1f);

        public UpgradeType UpgradeType => _definition.Type;
        
        private UpgradeDefinition _definition;
        private Action<UpgradeType> _onUpgrade;
        private Color _nameColor, _levelColor, _costColor, _flashBaseColor;
        private int _lastLevel = -1;


        void Awake()
        {
            _nameColor = _nameTxt.color;
            _levelColor = _levelTxt.color;
            _costColor = _costTxt.color;

            if (_flashGraphic)
            {
                _flashBaseColor = _flashGraphic.color;
            }
        }

        public void Init(UpgradeDefinition definition, Action<UpgradeType> onUpgrade)
        {
            if (_definition != definition)
            {
                _lastLevel = -1;
                ResetFeedback();
            }

            _definition = definition;
            _onUpgrade = onUpgrade;
        }

        public void Refresh(int level, int? upgradeCost, bool canUpgrade)
        {
            bool isMax = !upgradeCost.HasValue;
            bool purchased = _lastLevel >= 0 && level > _lastLevel;
            _lastLevel = level;

            _nameTxt.text = _definition.DisplayName;
            _levelTxt.text = $"Lvl. {level}/{_definition.MaxLevel}";
            _costTxt.text = isMax ? "MAX" : upgradeCost.Value.ToString();
            _upgradeBtn.interactable = canUpgrade;

            _nameTxt.color = isMax ? _maxColor : _nameColor;
            _levelTxt.color = isMax ? _maxColor : _levelColor;
            _costTxt.color = isMax ? _maxColor : canUpgrade ? _costColor : _unaffordableColor;

            if (purchased)
            {
                PlayPurchaseFeedback();
            }
        }

        public void OnUpgradeTouch()
        {
            _onUpgrade?.Invoke(_definition.Type);
        }

        private void PlayPurchaseFeedback()
        {
            ResetFeedback();
            transform.DOPunchScale(Vector3.one * _punchScale, _punchDuration).SetUpdate(true);

            if (_flashGraphic)
            {
                _flashGraphic.color = _flashColor;
                _flashGraphic.DOColor(_flashBaseColor, _flashDuration).SetUpdate(true);
            }
        }

        private void ResetFeedback()
        {
            transform.DOKill(true);

            if (_flashGraphic)
            {
                _flashGraphic.DOKill(true);
                _flashGraphic.color = _flashBaseColor;
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();

            if (_flashGraphic)
            {
                _flashGraphic.DOKill();
            }
        }
    }
}
