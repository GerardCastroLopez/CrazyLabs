using System.Collections.Generic;
using CrazyLabs.Gameplay.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gSDK.EventSystem;
using gSDK.MVC;
using TMPro;
using UnityEngine;

namespace CrazyLabs.Gameplay.UI
{
    public class GameplayUIView : BaseView<GameplayUIController>, IEventHandler<CollectibleCollectedEvent>
    {
        [SerializeField] TMP_Text _currencyTxt, _distanceTxt, _speedTxt;
        [SerializeField] GameObject _aimHint;
        [SerializeField] TMP_Text _pickupText;

        private readonly Stack<TMP_Text> _freeTexts = new();
        private int _activeTexts;
        private int _lastCurrency;
        
        
        public void OnPauseTouch()
        {
            _controller.Pause();
        }

        public void Handle(CollectibleCollectedEvent evt)
        {
            ShowPickupText(evt);
        }

        protected override UniTask InternalShow(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnBind()
        {
            base.OnBind();

            (transform as RectTransform).SetSafeArea();
            
            _controller.Currency.AddListener(OnCurrencyChanged, true);
            _controller.Distance.AddListener(f => _distanceTxt.text = $"{f:0} / {_controller.TrackLength:0} m", true);
            _controller.Speed.AddListener(f => _speedTxt.text = $"{f:0} km/h", true);
            _controller.ShowTutorial.AddListener(b => _aimHint.SetActive(b), true);

            if (_pickupText)
            {
                _pickupText.gameObject.SetActive(false);
            }

            EventDispatcher.Register(this);
        }

        private void OnCurrencyChanged(int currency)
        {
            _currencyTxt.text = currency.ToString();

            var tuning = _controller.PickupFeedback;
            _currencyTxt.transform.DOKill(true);

            if (currency > _lastCurrency)
            {
                _currencyTxt.transform.DOPunchScale(Vector3.one * tuning.HudPunchScale, tuning.HudPunchDuration);
            }

            _lastCurrency = currency;
        }

        private void ShowPickupText(CollectibleCollectedEvent evt)
        {
            var tuning = _controller.PickupFeedback;
            var cam = _controller.Camera;

            if (!_pickupText || !cam || _activeTexts >= tuning.MaxActiveTexts)
            {
                return;
            }

            var screen = cam.WorldToScreenPoint(evt.Position);

            if (screen.z <= 0f)
            {
                return;
            }

            var parent = _pickupText.transform.parent as RectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            {
                return;
            }

            var text = _freeTexts.Count > 0 ? _freeTexts.Pop() : Instantiate(_pickupText, parent);
            text.text = $"+{evt.Value}";
            text.gameObject.SetActive(true);

            var rect = text.rectTransform;
            rect.anchoredPosition = local;
            rect.localScale = Vector3.one * tuning.TextStartScale;
            text.alpha = 1f;
            _activeTexts++;

            DOTween.Sequence()
                .Join(rect.DOAnchorPosY(local.y + tuning.TextRiseDistance, tuning.TextDuration).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(1f, tuning.TextDuration * 0.3f).SetEase(Ease.OutBack))
                .Join(DOTween.To(() => text.alpha, a => text.alpha = a, 0f, tuning.TextDuration * 0.4f).SetDelay(tuning.TextDuration * 0.6f))
                .SetTarget(text)
                .OnComplete(() => RecycleText(text));
        }

        private void RecycleText(TMP_Text text)
        {
            if (!text)
            {
                return;
            }

            text.gameObject.SetActive(false);
            _freeTexts.Push(text);
            _activeTexts--;
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
