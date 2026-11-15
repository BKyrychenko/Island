using System.Windows;
using Isle.Core;

namespace Isle;

// A full-panel overlay above the tabs (command bar). Hidden again when the island collapses.
public partial class IslandWindow
{
    FrameworkElement? _overlay;

    public bool IsOverlayShown(FrameworkElement view) => ReferenceEquals(_overlay, view) && view.Visibility == Visibility.Visible && _expanded;

    public void ShowOverlay(FrameworkElement view)
    {
        if (!ReferenceEquals(_overlay, view))
        {
            if (_overlay is not null) Panel.Children.Remove(_overlay);
            _overlay = view;
            System.Windows.Controls.Panel.SetZIndex(view, 5);
            Panel.Children.Add(view);
        }
        HideMoreOverlay();
        view.Visibility = Visibility.Visible;
        if (!_expanded) Expand(sticky: true);
        view.Dispatcher.BeginInvoke(ApplyExStyle, System.Windows.Threading.DispatcherPriority.Loaded);
        Safe(() => (view as ITabView)?.OnShown(), "overlay shown");
    }

    public void HideOverlay()
    {
        if (_overlay is not { Visibility: Visibility.Visible } o) return;
        o.Visibility = Visibility.Collapsed;
        ApplyExStyle();
        Safe(() => (o as ITabView)?.OnHidden(), "overlay hidden");
    }

    /// <summary>Keeps the island out of screen captures (our own screenshots/recordings) while <paramref name="on"/>.</summary>
    public void ExcludeFromCapture(bool on)
    {
        if (_hwnd != IntPtr.Zero) Native.SetWindowDisplayAffinity(_hwnd, on ? Native.WDA_EXCLUDEFROMCAPTURE : Native.WDA_NONE);
    }
}
