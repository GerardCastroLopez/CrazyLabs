using System;
using UnityEngine;

namespace gSDK.UI
{
    public interface IPopupBackground
    {
        RectTransform Rect { get; }
        Action OnTouch { get; set; }

        void Toggle(bool toggle);
        void Refresh();
        void ForceHide();
    }
}