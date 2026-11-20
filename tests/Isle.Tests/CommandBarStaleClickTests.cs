using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Isle.Modules.Commands;
using Isle.Views.Commands;

namespace Isle.Tests;

public class CommandBarStaleClickTests
{
    sealed class Provider : ICommandProvider
    {
        public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct) =>
            Task.FromResult<IEnumerable<CommandItem>>([new() { Title = text, Run = () => { } }]);
    }

    static void Click(UIElement row) => row.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
    {
        RoutedEvent = UIElement.MouseLeftButtonUpEvent
    });

    [Fact]
    public void Clicking_a_previous_query_row_during_debounce_does_not_run_the_new_command()
    {
        FakeSink.Run(sink =>
        {
            var dispatched = new List<string>();
            var view = new CommandBarView([new Provider()], item => dispatched.Add(item.Title), null);
            var rows = (StackPanel)((ScrollViewer)view.Children[1]).Content;
            view.Text = "=4";
            Assert.True(sink.PumpUntil(() => view.Results.Count == 1));
            var previous = (Border)rows.Children[0];

            view.Text = "timer 5m";
            var detachedOnEdit = previous.Parent is null;
            Click(previous);
            Assert.True(sink.PumpUntil(() => view.Results.FirstOrDefault()?.Title == "timer 5m"));
            Assert.Empty(dispatched);
            Assert.True(detachedOnEdit);

            Click(rows.Children[0]);
            Assert.Equal(["timer 5m"], dispatched);
        });
    }
}
