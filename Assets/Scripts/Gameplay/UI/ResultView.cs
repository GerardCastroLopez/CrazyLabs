using CrazyLabs.MainMenu.UI;
using CrazyLabs.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gSDK.MVC;
using TMPro;
using UnityEngine;

namespace CrazyLabs.Gameplay.UI
{
    public class ResultView : BaseView<ResultController>
    {
        [SerializeField] private CanvasGroup _canvasGrp;
        [SerializeField] private TweenValues _tween;
        [SerializeField] private TMP_Text _resultTxt, _resultDetailsTxt;
        [SerializeField] private string _winTitle = "Course Complete!";
        [SerializeField] private string _loseTitle = "Run Over";
        [SerializeField] private UpgradesComponent _upgrades;


        public void OnRetryTouch()
        {
            _controller.Retry();
        }

        public void OnHomeTouch()
        {
            _controller.Forfeit();
        }

        protected override void OnBind()
        {
            base.OnBind();

            _upgrades.Init(_controller.UpgradesModule, _controller.Currency);
        }
        
        protected override UniTask InternalShow(bool animate)
        {
            DOTween.Kill(this);

            if (animate)
            {
                _canvasGrp.alpha = 0f;
                return _canvasGrp.DOFade(1f, _tween.Duration).SetId(this).SetEase(_tween.Ease).AsyncWaitForCompletion().AsUniTask();
            }

            _canvasGrp.alpha = 1f;
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            DOTween.Kill(this);
            
            if (animate)
            {
                return _canvasGrp.DOFade(0f, _tween.Duration).SetId(this).SetEase(_tween.Ease).AsyncWaitForCompletion().AsUniTask();
            }

            _canvasGrp.alpha = 0f;
            return UniTask.CompletedTask;
        }

        protected override UniTask WillShow()
        {
            var result = _controller.Result;

            _resultTxt.text = result.IsWin ? _winTitle : _loseTitle;
            _resultDetailsTxt.text = $"Distance  {result.DistanceMeters:0} m  ({result.Progress01 * 100f:0}%)\nCroissants  +{result.CoinsCollected}";

            return base.WillShow();
        }
    }
}
