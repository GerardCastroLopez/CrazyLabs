namespace gSDK.MVC
{
    public interface IPopupViewController : IBaseViewController
    {
        bool CanCloseAutomatically { get; set; }
        int UILayer { get; }
        void LoseFocus();
        void RegainFocus();
        void ClosePopup();
        void MoveToFront();
    }
}