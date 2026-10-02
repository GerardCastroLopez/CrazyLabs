using System;
using Cysharp.Threading.Tasks;
using gSDK.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace gSDK.MVC
{
    public class UIViewController<TView> : BaseViewController<TView>, IBaseViewController<TView> where TView : BaseView
    {
        protected readonly UIService _uiService;
        private readonly int _uiLayer;
        [CanBeNull] private Type _singleInstanceType;


        public UIViewController(object viewRef, int uiLayer, bool destroyOnHide, UIService uiService) : base(viewRef, destroyOnHide)
        {
            _uiLayer = uiLayer;
            _uiService = uiService;
            ViewState.AddListener(OnViewStateChanged);

            if (!_destroyOnHide)
            {
                _singleInstanceType = GetType();
            }
        }

        protected void DoNotBlockInput()
        {
            ViewState.RemoveListener(OnViewStateChanged);
        }

        private void OnViewStateChanged(eViewState state)
        {
            _uiService.BlockTouch(!state.Is(eViewState.IsShownOrHidden), this);
        }

        protected override UniTask<GameObject> InstantiateAsync()
        {
            return _uiService.Show(_viewRef, _uiLayer, _singleInstanceType);
        }
    }

    public class UIViewController : UIViewController<BaseView>
    {
        public UIViewController(object viewRef, int uiLayer, bool destroyOnHide, UIService uiService) : base(viewRef, uiLayer, destroyOnHide, uiService)
        {}
    }
}