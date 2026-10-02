using Cysharp.Threading.Tasks;

namespace gSDK.MVC
{
    public enum eViewState
    {
        Hidden = 1 << 0,
        Showing = 1 << 1,
        Shown = 1 << 2,
        Hiding = 1 << 3,

        IsShowingOrShown = Showing | Shown,
        IsHidingOrHidden = Hiding | Hidden,
        IsAnimating = Showing | Hiding,
        IsShownOrHidden = Shown | Hidden,
    }

    public static class eViewStateExtension
    {
        public static bool Is(this eViewState state, eViewState toCheck)
        {
            return (toCheck & state) == state;
        }
    }

    public interface IBaseViewController
    {
        Reactive<eViewState> ViewState { get; }
        UniTask CacheView();
        UniTask ShowView(bool animate, bool force = false);
        UniTask HideView(bool animate, bool force = false);
        void Destroy();
    }

    public interface IBaseViewController<TView> : IBaseViewController where TView : BaseView
    {
        TView View { get; }
    }
}