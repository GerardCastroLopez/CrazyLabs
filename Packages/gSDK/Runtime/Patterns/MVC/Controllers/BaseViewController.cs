using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.MVC
{
    public abstract class BaseViewController<TView> : IBaseViewController where TView : BaseView
    {
        public TView View { get; private set; }

        public Reactive<eViewState> ViewState { get; } = new(eViewState.Hidden);

        protected readonly bool _destroyOnHide;
        protected readonly object _viewRef;


        public BaseViewController(object viewRef, bool destroyOnHide)
        {
            _viewRef = viewRef;
            _destroyOnHide = destroyOnHide;
        }

        public virtual async UniTask CacheView()
        {
            if (View)
            {
                return;
            }

            var go = await InstantiateAsync();

            if (!go)
            {
                Debug.LogError($"Instantiate error for {GetType().Name}");
            }
            else
            {
                View = go.GetComponent<TView>();

                if (!View)
                {
                    Debug.LogError($"Binding incompatibility: {go.name} doesn't contain a view {typeof(TView).Name} for the controller {GetType().Name}");
                    Object.Destroy(go);
                    return;
                }

                OnBindInternal();
                
                // could happen that the view loaded after it was requested to hide
                if (ViewState.Value.Is(eViewState.IsHidingOrHidden))
                {
                    HideView(false, true).Forget();
                }
            }
        }

        public virtual async UniTask ShowView(bool animate, bool force = false)
        {
            if (!force && ViewState.Value.Is(eViewState.IsShowingOrShown))
            {
                return;
            }

            ViewState.Value = eViewState.Showing;

            if (!View)
            {
                await CacheView();
            }

            if (ViewState.Value == eViewState.Showing)
            {
                await View.Show(animate);

                ViewState.Value = eViewState.Shown;
            }
        }

        public virtual async UniTask HideView(bool animate, bool force = false)
        {
            if (!force && ViewState.Value.Is(eViewState.IsHidingOrHidden))
            {
                return;
            }

            ViewState.Value = eViewState.Hiding;

            if (View)
            {
                await View.Hide(animate);
            }

            ViewState.Value = eViewState.Hidden;

            if (_destroyOnHide)
            {
                Destroy();
            }
        }

        public virtual void Destroy()
        {
            View?.Destroy();
            View = null;
        }

        protected abstract UniTask<GameObject> InstantiateAsync();

        protected virtual void OnBindInternal()
        {
            View.Bind(this);
        }
    }
}