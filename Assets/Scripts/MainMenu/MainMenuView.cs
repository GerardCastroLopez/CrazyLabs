using CrazyLabs.Characters;
using CrazyLabs.MainMenu.UI;
using CrazyLabs.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gSDK.MVC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.MainMenu
{
    public class MainMenuView : BaseView<MainMenuController>
    {
        [SerializeField] private CanvasGroup _canvasGrp;
        [SerializeField] private TweenValues _showTween, _hideTween;
        [SerializeField] private Image _characterImg;
        [SerializeField] private TMP_Text _characterNameTxt, _levelNameTxt;
        [SerializeField] private UpgradesComponent _upgrades;
        [SerializeField] private AudioSource _audioSource;
        

        public void OnPlayTouch()
        {
            _controller.Play();
        }

        public void OnChangeCharacter(int delta)
        {
            _controller.ChangeCharacter(delta);
        }

        public void OnChangeLevel(int delta)
        {
            _controller.ChangeLevel(delta);
        }

        protected override void OnBind()
        {
            base.OnBind();
            
            _controller.Character.AddListener(OnCharacterChanged, true);
            _controller.Level.AddListener(l => _levelNameTxt.text = l.DisplayName, true);
            _upgrades.Init(_controller.UpgradesModule, _controller.Currency);
        }

        protected override UniTask InternalShow(bool animate)
        {
            DOTween.Kill(this);
            
            if (animate)
            {
                _canvasGrp.alpha = 0f;
                return _canvasGrp.DOFade(1f, _showTween.Duration).SetEase(_showTween.Ease).SetId(this).AsyncWaitForCompletion().AsUniTask();
            }
            
            _canvasGrp.alpha = 1f;
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            DOTween.Kill(this);
            
            if (animate)
            {
                return _canvasGrp.DOFade(0f, _hideTween.Duration).SetEase(_hideTween.Ease).SetId(this).AsyncWaitForCompletion().AsUniTask();
            }
            
            _canvasGrp.alpha = 0f;
            return UniTask.CompletedTask;
        }

        private void OnCharacterChanged(CharacterData character)
        {
            bool hasChanged = !string.IsNullOrEmpty(_characterNameTxt.text);
            
            //_characterImg.sprite = character.Icon;
            _characterNameTxt.text = character.Name;

            if (hasChanged)
            {
                _audioSource.PlayOneShot(character.SelectedClip);
            }
        }
    }
}