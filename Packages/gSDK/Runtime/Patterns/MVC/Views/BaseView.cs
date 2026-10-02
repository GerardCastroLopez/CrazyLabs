using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.MVC
{
    public abstract class BaseView : MonoBehaviour
    {
        public abstract void Bind(IBaseViewController controller);

        
        internal async UniTask Show(bool animate)
        {
            await WillShow();

            gameObject.SetActive(true);

            await InternalShow(animate);

            Shown();
        }

        protected virtual UniTask WillShow()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void Shown()
        {}

        protected abstract UniTask InternalShow(bool animate);

        internal async UniTask Hide(bool animate)
        {
            WillHide();

            await InternalHide(animate);

            gameObject.SetActive(false);

            Hidden();
        }

        protected virtual void WillHide()
        {}

        protected virtual void Hidden()
        {}

        protected abstract UniTask InternalHide(bool animate);

        protected internal virtual void Destroy()
        {
            Destroy(gameObject);
        }
    }

    public abstract class BaseView<TController> : BaseView where TController : IBaseViewController
    {
        protected TController _controller;


        public sealed override void Bind(IBaseViewController controller)
        {
            if(controller is TController tController)
            {
                _controller = tController;
                OnBind();
            }
            else
            {
                Debug.LogError($"Binding incompatibility: controller {controller.GetType().Name}, view {this.GetType().Name}", this);
            }
        }

        protected virtual void OnBind()
        { }
    }
}