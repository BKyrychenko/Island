using System.Windows;

namespace Skerry.Core;

/// <summary>A tab of the expanded panel. Registered by its module on Start and removed on Stop.</summary>
/// <param name="Id">Stable id, also stored as "last tab".</param>
/// <param name="TitleKey">Strings key of the label.</param>
/// <param name="IconKey">Resource key in Styles/Icons.xaml, e.g. "Icon.Music".</param>
/// <param name="CreateView">Builds the view on first show (lazy).</param>
/// <param name="Order">Position in the tab bar (spec order: 10 Music … 100 Agents).</param>
public sealed record TabInfo(string Id, string TitleKey, string IconKey, Func<FrameworkElement> CreateView, int Order);

/// <summary>Optional interface for tab views that run timers/refresh only while actually on screen.</summary>
public interface ITabView
{
    /// <summary>The island is expanded and this tab is the visible one.</summary>
    void OnShown();
    /// <summary>The island collapsed or another tab was selected.</summary>
    void OnHidden();
}
