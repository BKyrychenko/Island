using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Skerry.Core;
using Skerry.Modules.Weather;
using Skerry.Views.Weather;

namespace Skerry.Tests;

public class WeatherSearchTests
{
    sealed record Lookup(string Query, CancellationToken Token, TaskCompletionSource<List<Place>> Reply);

    sealed class Setup
    {
        public readonly List<Lookup> Requests = [];
        public readonly List<Place> Picks = [];
        public readonly WeatherView View;
        public readonly TextBox City;
        public readonly TextBlock Status;
        public readonly StackPanel Rows;
        readonly Button _search;

        public Setup()
        {
            View = new WeatherView(new(Search, Picks.Add, _ => { }, () => { }, _ => { }, _ => { }, _ => { }));
            View.ShowSetup(false);
            var grid = (Grid)((ContentControl)View.Children[0]).Content;
            var top = (DockPanel)grid.Children[0];
            City = ((Grid)top.Children[^1]).Children.OfType<TextBox>().Single();
            _search = top.Children.OfType<Button>().Single();
            var lower = (StackPanel)((ScrollViewer)grid.Children[2]).Content;
            Status = (TextBlock)lower.Children[0];
            Rows = (StackPanel)lower.Children[1];
        }

        Task<List<Place>> Search(string query, CancellationToken token)
        {
            var lookup = new Lookup(query, token, new());
            Requests.Add(lookup);
            return lookup.Reply.Task;
        }

        public Lookup Submit(string city)
        {
            City.Text = city;
            _search.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var request = Requests[^1];
            Assert.Equal(city, request.Query);
            return request;
        }
    }

    [Fact]
    public void Older_geocoder_reply_cannot_replace_or_select_a_newer_city()
    {
        FakeSink.Run(sink => sink.Ui.Invoke(() =>
        {
            var setup = new Setup();
            var older = setup.Submit("Paris");
            var newer = setup.Submit("Parma");
            Assert.True(older.Token.IsCancellationRequested);
            Assert.False(newer.Token.IsCancellationRequested);

            var parma = new Place("Parma", "Italy", 44.8, 10.3);
            newer.Reply.SetResult([parma]);
            older.Reply.SetResult([new Place("Paris", "France", 48.9, 2.4)]);
            sink.Ui.Invoke(() => { }, DispatcherPriority.Background);
            Assert.Equal(Strings.T("wx.pick"), setup.Status.Text);
            var row = Assert.Single(setup.Rows.Children.OfType<Button>());
            row.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(parma, Assert.Single(setup.Picks));

            setup.City.Text = "Pa";
            Assert.Empty(setup.Rows.Children);
            row.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Single(setup.Picks);
        }, DispatcherPriority.Normal));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("hide")]
    [InlineData("leave setup")]
    public void Interrupted_lookup_cannot_publish_after_return(string action)
    {
        FakeSink.Run(sink => sink.Ui.Invoke(() =>
        {
            var setup = new Setup();
            var hint = setup.Status.Text;
            var pending = setup.Submit("Kyiv");
            switch (action)
            {
                case "edit":
                    setup.City.Text = "K";
                    setup.City.Text = "Kyiv";
                    break;
                case "hide":
                    setup.View.OnHidden();
                    setup.View.OnShown();
                    break;
                case "leave setup":
                    setup.View.ShowLoading();
                    setup.View.ShowSetup(false);
                    break;
            }
            Assert.True(pending.Token.IsCancellationRequested);
            pending.Reply.SetResult([new Place("Kyiv", "Ukraine", 50.5, 30.5)]);
            sink.Ui.Invoke(() => { }, DispatcherPriority.Background);
            Assert.Equal(hint, setup.Status.Text);
            Assert.Empty(setup.Rows.Children);
            Assert.Empty(setup.Picks);
        }, DispatcherPriority.Normal));
    }

    [Fact]
    public void Retrying_the_same_query_survives_the_first_request_acknowledging_cancellation()
    {
        FakeSink.Run(sink => sink.Ui.Invoke(() =>
        {
            var setup = new Setup();
            var first = setup.Submit("Tokyo");
            var retry = setup.Submit("Tokyo");
            Assert.True(first.Token.IsCancellationRequested);
            first.Reply.SetCanceled(first.Token);
            sink.Ui.Invoke(() => { }, DispatcherPriority.Background);
            Assert.Equal(Strings.T("wx.searching"), setup.Status.Text);
            Assert.Empty(setup.Rows.Children);

            retry.Reply.SetResult([]);
            sink.Ui.Invoke(() => { }, DispatcherPriority.Background);
            Assert.Equal(Strings.T("wx.noResults"), setup.Status.Text);
        }, DispatcherPriority.Normal));
    }
}
