using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using gSDK.MVC;

namespace CrazyLabs.UI
{
    public abstract class PopupView<T> : BaseView<T> where T : IPopupViewController
    {
        public enum eMovementDirection { Top, Bottom, Left, Right }

        private Dictionary<eMovementDirection, Vector2> _movementDirection = new() {

            { eMovementDirection.Top, Vector2.up },
            { eMovementDirection.Bottom, Vector2.down },
            { eMovementDirection.Left, Vector2.left },
            { eMovementDirection.Right, Vector2.right }
        };

        [SerializeField]
        protected Button _closeBtn;
        [SerializeField]
        private eMovementDirection _showFromDirection = eMovementDirection.Top, _hideToDirection = eMovementDirection.Bottom;
        [SerializeField]
        private Ease _showEase = Ease.OutBack, _hideEase = Ease.InQuart;
        [SerializeField]
        private float _animationDuration = 0.5f;

        protected RectTransform _rect, _parent;


        protected override void OnBind()
        {
            base.OnBind();
            
            _rect = transform as RectTransform;
            _parent = transform.parent as RectTransform;

            _controller.CanCloseAutomatically = _closeBtn != null;
            _closeBtn?.onClick.AddListener(ClosePopup);

            _rect.anchoredPosition = GetHiddenPosition(_showFromDirection);
        }

        protected override UniTask InternalShow(bool animate)
        {
            DOTween.Kill(_rect);

            _rect.anchoredPosition = GetHiddenPosition(_showFromDirection);
            return _rect.DOAnchorPos(Vector2.zero, animate ? _animationDuration : 0f).SetId(_rect).SetUpdate(true).SetEase(_showEase).AsyncWaitForCompletion().AsUniTask();
        }

        protected override UniTask InternalHide(bool animate)
        {
            DOTween.Kill(_rect);

            _rect.anchoredPosition = Vector2.zero;
            var toPos = GetHiddenPosition(_hideToDirection);
            return _rect.DOAnchorPos(toPos, animate ? _animationDuration : 0f).SetId(_rect).SetUpdate(true).SetEase(_hideEase).AsyncWaitForCompletion().AsUniTask();
        }

        private Vector2 GetHiddenPosition(eMovementDirection direction)
        {
            var halfSize = (_parent.rect.size + _rect.rect.size) /2f;
            return halfSize * _movementDirection[direction];
        }

        protected virtual void ClosePopup()
        {
            _controller.ClosePopup();
        }
    }
}