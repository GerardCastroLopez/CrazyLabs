using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using gSDK.UI;

namespace CrazyLabs.UI
{
    public class PopupBackground : MonoBehaviour, IPopupBackground
    {
        public RectTransform Rect { get; private set; }
        public Action OnTouch { get; set; }

        [SerializeField]
        private Graphic _graphic;
        [SerializeField]
        private float _animationDuration;

        private float _shownAlpha;


        void Awake()
        {
            Rect = transform as RectTransform;
            _shownAlpha = _graphic.color.a;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Touched();
            }
        }

        public void Toggle(bool toggle)
        {
            gameObject.SetActive(true);

            float alpha = toggle ? 0f : _shownAlpha;

            DOTween.Kill(_graphic);
            _graphic.SetAlpha(alpha);

            alpha = toggle ? _shownAlpha : 0f;

            _graphic.DOFade(alpha, _animationDuration).SetId(_graphic).SetUpdate(true).OnComplete(() => {

                gameObject.SetActive(toggle);
            });
        }

        public void Refresh()
        {}

        public void Touched()
        {
            OnTouch?.Invoke();
        }

        public void ForceHide()
        {
            gameObject.SetActive(false);
        }
    }
}