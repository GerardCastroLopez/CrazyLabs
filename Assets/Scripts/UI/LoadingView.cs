using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gSDK.UI;
using UnityEngine;

namespace CrazyLabs.UI
{
    public class LoadingView : MonoBehaviour, ILoadingView
    {
        [SerializeField]
        private CanvasGroup _canvasGrp;

        [SerializeField]
        private float _animationDuration;


        public async UniTask AsyncToggle(bool toggle, bool animate)
        {
            await CommonToggle(toggle, animate, null).AsyncWaitForCompletion().AsUniTask();
        }
        
        public void Toggle(bool toggle, bool animate, Action callback)
        {
            CommonToggle(toggle, animate, callback);
        }

        private Tween CommonToggle(bool toggle, bool animate, Action callback)
        {
            gameObject.SetActive(true);

            float alpha = toggle ? 1f : 0f;
            float duration = animate ? _animationDuration : 0f;

            DOTween.Kill(this);
            return _canvasGrp.DOFade(alpha, duration).SetId(this).SetUpdate(true).OnComplete(() => {
               
                gameObject.SetActive(toggle);
                callback?.Invoke();
            });
        }

        public void MoveToFront()
        {
            transform.SetAsLastSibling();
        }
    }
}
