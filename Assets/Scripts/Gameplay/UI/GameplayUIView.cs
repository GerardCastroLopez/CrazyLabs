using Cysharp.Threading.Tasks;
using gSDK.MVC;
using TMPro;
using UnityEngine;

namespace CrazyLabs.Gameplay.UI
{
    public class GameplayUIView : BaseView<GameplayUIController>
    {
        [SerializeField] TMP_Text _currencyTxt, _distanceTxt, _speedTxt;
        [SerializeField] GameObject _aimHint;
        
        
        public void OnPauseTouch()
        {
            _controller.Pause();
        }

        protected override UniTask InternalShow(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnBind()
        {
            base.OnBind();
            
            _controller.Currency.AddListener(i => _currencyTxt.text = i.ToString(), true);
            _controller.Distance.AddListener(f => _distanceTxt.text = $"{f:0} / {_controller.TrackLength:0} m", true);
            _controller.Speed.AddListener(f => _speedTxt.text = $"{f:0} km/h", true);
            _controller.ShowTutorial.AddListener(b => _aimHint.SetActive(b), true);
        }
    }
}