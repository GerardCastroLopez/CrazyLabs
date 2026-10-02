using System;
using Cysharp.Threading.Tasks;
using gSDK.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace gSDK.MVC
{
    public class PopupViewController<TView> : BaseViewController<TView>, IPopupViewController, IBaseViewController<TView> where TView : BaseView
    {
        public bool CanCloseAutomatically { get; set; } = true;
        public int UILayer { get; private set; }

        protected PopupService _popupService;
        [CanBeNull] private readonly Type _singleInstanceType;

        
        public PopupViewController(object viewRef, int uiLayer, bool destroyOnHide, PopupService popupService) : base(viewRef, destroyOnHide)
        {
            UILayer = uiLayer;
            _popupService = popupService;
            ViewState.AddListener(OnViewStateChanged);

            if (!_destroyOnHide)
            {
                _singleInstanceType = GetType();
            }
        }

        private void OnViewStateChanged(eViewState state)
        {
            _popupService.BlockTouch(state.Is(eViewState.IsAnimating), this);

            if (state == eViewState.Showing)
            {
                _popupService.ShowingPopup(this);
            }
            else if (state == eViewState.Hidden)
            {
                _popupService.PopupClosed(this);
            }
        }

        public virtual void RegainFocus()
        {}

        public virtual void LoseFocus()
        {}

        /// <summary>
        /// Used by player's interaction
        /// </summary>
        public virtual void ClosePopup()
        {
            HideView(true).Forget();
        }

        public void MoveToFront()
        {
            View?.transform.SetAsLastSibling();
        }

        protected override UniTask<GameObject> InstantiateAsync()
        {
            return _popupService.InstantiatePopup(_viewRef, UILayer, _singleInstanceType);
        }
    }

    public class PopupViewController : PopupViewController<BaseView>
    {
        public PopupViewController(object viewRef, int uiLayer, bool destroyOnHide, PopupService popupService) : base(viewRef, uiLayer, destroyOnHide, popupService)
        {}
    }
}