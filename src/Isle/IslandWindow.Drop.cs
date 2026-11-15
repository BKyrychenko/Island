using System.Windows;

namespace Isle;

// Files dragged onto the island: it expands to the shelf tab and hands the paths to the shelf module.
public partial class IslandWindow
{
    /// <summary>Set by the shelf module: tab to open on drag-over and what to do with dropped paths. Null = drops refused.</summary>
    public (string TabId, Action<string[]> Drop, Action<bool> Hint)? FileDropTarget { get; set; }

    void InitDrop()
    {
        AllowDrop = true;
        DragEnter += OnDragOver;
        DragOver += OnDragOver;
        DragLeave += (_, _) => FileDropTarget?.Hint(false);
        Drop += OnDrop;
    }

    void OnDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (FileDropTarget is not { } t || !e.Data.GetDataPresent(DataFormats.FileDrop) || _holdOpen.Contains("shelf-drag"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Link | DragDropEffects.Copy;
        if (!_expanded) Expand(t.TabId, sticky: true);
        else SelectTab(t.TabId, animate: true);
        t.Hint(true);
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (FileDropTarget is not { } t) return;
        t.Hint(false);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            Safe(() => t.Drop(files), "shelf drop");
    }
}
