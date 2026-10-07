using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.MVC
{
    public abstract class ViewController<TView> : BaseViewController<TView> where TView : BaseView
    {
        private Transform _parentTr;


        protected ViewController(object viewRef, bool destroyOnHide) : base(viewRef, destroyOnHide)
        {}

        public void SetParent(Transform parent)
        {
            _parentTr = parent;

            View?.transform.SetParent(_parentTr, false);
        }

        protected override UniTask<GameObject> InstantiateAsync()
        {
            return AddressableInstance.Instantiate(_viewRef, _parentTr);
        }
    }

    public class ViewController : ViewController<BaseView>
    {
        public ViewController(object viewRef, bool destroyOnHide) : base(viewRef, destroyOnHide)
        {}
    }
}