using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using gSDK.MVC;
using gSDK.Services;
using JetBrains.Annotations;

namespace gSDK.UI
{
    public class PopupService : BaseService
    {
        private UIService _uiService;
        private IPopupBackground _background;
        private List<IPopupViewController> _popups = new();
        private object _popupBackgroundAssetRef;

        
        public PopupService(object popupBackgroundAssetRef) : base(true)
        {
            _popupBackgroundAssetRef = popupBackgroundAssetRef;
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();

            _uiService = _locator.GetService<UIService>();
            _uiService.OnReady(() => LoadAsync().Forget());
        }

        protected override void Ready()
        {
            if (_uiService != null && _background != null)
            {
                base.Ready();
            }
        }

        public UniTask<GameObject> InstantiatePopup(object viewRef, int layerIndex, [CanBeNull] Type singleInstanceType)
        {
            _uiService.SetLayer(_background.Rect, layerIndex);
            return _uiService.Show(viewRef, layerIndex, singleInstanceType);
        }

        public void BlockTouch(bool value, object requester)
        {
            _uiService.BlockTouch(value, requester);
        }

        public void ShowingPopup(IPopupViewController popup)
        {
            if(popup == null)
            {
                Debug.LogError("Tried to open a null popup");
                return;
            }

            if (_popups.Count > 0)
            {
                _popups[^1].LoseFocus();
            }
            else
            {
                _background.Toggle(true);
            }

            MoveToFront(popup);

            _popups.Add(popup);
        }

        public void PopupClosed(IPopupViewController popup)
        {
            if (popup == null)
            {
                Debug.LogError("Tried to close a null popup");
                return;
            }

            if(_popups.Contains(popup))
            {
                _popups.Remove(popup);
            }

            if (_popups.Count > 0)
            {
                var prevPopup = _popups[^1];

                MoveToFront(prevPopup);
                
                prevPopup.RegainFocus();
            }
            else
            {
                _background.Toggle(false);
            }
        }

        public void CloseAllPopups()
        {
            foreach(var popup in _popups)
            {
                popup.HideView(false).Forget();
            }
            _popups.Clear();
        }

        private void MoveToFront(IPopupViewController popup)
        {
            _uiService.SetLayer(_background.Rect, popup.UILayer);
            _background.Rect.SetAsLastSibling();
            _background.Refresh();

            popup.MoveToFront();
        }

        private void CloseCurrentPopup()
        {
            if (_popups.Count > 0)
            {
                var lastPopup = _popups[^1];

                if (lastPopup.ViewState.Value == eViewState.Shown && lastPopup.CanCloseAutomatically)
                {
                    lastPopup.ClosePopup();
                }
            }
        }

        private async UniTask LoadAsync()
        {
            var go = await _uiService.Show(_popupBackgroundAssetRef, 0, null);

            go.transform.SetFullScreen();
            _background = go.GetComponent<IPopupBackground>();
            _background.OnTouch = CloseCurrentPopup;
            _background.ForceHide();

            Ready();
        }
    }
}