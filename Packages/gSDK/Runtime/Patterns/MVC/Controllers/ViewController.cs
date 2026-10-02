using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        protected override async UniTask<GameObject> InstantiateAsync()
        {
            var prefab = await Addressables.LoadAssetAsync<GameObject>(_viewRef).Task.AsUniTask();
            return Object.Instantiate(prefab, _parentTr);
        }
    }

    public class ViewController : ViewController<BaseView>
    {
        public ViewController(object viewRef, bool destroyOnHide) : base(viewRef, destroyOnHide)
        {}
    }
}