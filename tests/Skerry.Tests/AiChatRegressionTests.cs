using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Skerry.Modules.AiChat;
using Skerry.Views.AiChat;

namespace Skerry.Tests;

public class AiChatRegressionTests
{
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;
    static void SetField(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(obj, value);

    sealed class DelayedProvider(bool fail = false, bool partial = false) : IChatProvider
    {
        public readonly TaskCompletionSource Ready = new();
        public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
        {
            if (partial) yield return "First words";
            await Ready.Task.WaitAsync(ct);
            if (fail) throw new AiException("ai.err.codexLogin");
            yield return "Offline answer";
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void StopOrFailureKeepsVisibleBubblesInSyncWithSavedHistory(bool stop, bool partial)
    {
        using var dir = new TempDir();
        var file = Path.Combine(dir.Path, "chats.json");
        FakeSink.Run(sink =>
        {
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(sink.Ui));
            var store = new ChatStore(file);
            var chat = store.New();
            chat.Messages.Add(new ChatMessage { Role = "user", Text = "Earlier question" });
            chat.Messages.Add(new ChatMessage { Role = "assistant", Text = string.Join("\n", Enumerable.Repeat("Prior reply", 40)) });
            var provider = new DelayedProvider(fail: true, partial: partial);
            var module = new AiModule(null!, _ => provider, store);
            var view = new AiChatView(module);
            SetField(module, "_view", view);
            SetField(module, "_current", chat);
            view.ShowChat(chat);
            view.SetInput("Current question");
            var send = Field<Button>(view, "_send");
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(module.Busy);
            if (partial) view.SetInput("Next draft");

            void Layout()
            {
                view.Measure(new Size(420, 180));
                view.Arrange(new Rect(0, 0, 420, 180));
                view.UpdateLayout();
                sink.Ui.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            Layout();
            var scroll = Field<ScrollViewer>(view, "_scroll");
            Assert.True(scroll.ScrollableHeight > 100);
            scroll.ScrollToVerticalOffset(32);
            Layout();
            Assert.Equal(32, scroll.VerticalOffset);

            if (stop) send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            else provider.Ready.SetResult();
            Assert.True(sink.PumpUntil(() => !module.Busy));
            Layout();
            Assert.Equal(32, scroll.VerticalOffset);
            Assert.Equal(partial ? "Next draft" : "Current question", Field<TextBox>(view, "_input").Text);

            var saved = new ChatStore(file).Get(chat.Id)!.Messages;
            var bubbles = Field<StackPanel>(view, "_messages").Children.Cast<Border>().ToList();
            Assert.Equal(partial ? 4 : 3, saved.Count);
            Assert.Equal(saved.Count, bubbles.Count);
            static string ShownText(StackPanel panel)
            {
                var block = Assert.Single(panel.Children.OfType<TextBlock>());
                return new TextRange(block.ContentStart, block.ContentEnd).Text;
            }
            var questions = bubbles.Where(b => b.HorizontalAlignment == HorizontalAlignment.Right)
                .Select(b => ShownText((StackPanel)b.Child));
            Assert.Equal(saved.Where(m => m.Role == "user").Select(m => m.Text), questions);
            var answer = (StackPanel)bubbles[^1].Child;
            var status = Skerry.Core.Strings.T(stop ? "ai.stopped" : "ai.err.codexLogin");
            Assert.Equal(status, ShownText(answer));
            Assert.Equal(stop, saved[^1].Stopped);
            Assert.Equal(!stop && !partial, saved[^1].Error);
            Assert.Equal(partial ? "First words" : stop ? "" : status, saved[^1].Text);
            if (partial)
                Assert.Equal(saved[^1].Text, ShownText((StackPanel)answer.Children[0]));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstSendMovesTheComposerToItsChatAndDeletionReturnsToAnEmptyDraft(bool editWhileSending)
    {
        var file = Path.Combine(Path.GetTempPath(), "skerry-first-chat-" + Guid.NewGuid() + ".json");
        var lateFile = Path.ChangeExtension(file, ".txt");
        try
        {
            FakeSink.Run(sink =>
            {
                SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(sink.Ui));
                var store = new ChatStore(file);
                var provider = new DelayedProvider();
                var module = new AiModule(null!, _ => provider, store);
                var view = new AiChatView(module);
                SetField(module, "_view", view);
                view.ShowChat(null);
                var input = Field<TextBox>(view, "_input");
                var chips = Field<WrapPanel>(view, "_chips");
                view.SetInput("First question");
                var sent = new ChatAttachment { Name = "first.txt", Kind = AttachmentKind.Text, Text = "First" };
                view.AddPending(sent);
                var attachmentTarget = Field<object>(view, "_draft"); // Captured when the picker opens.
                Field<Button>(view, "_send").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var chat = Assert.Single(store.All);
                Assert.True(module.Busy);
                if (editWhileSending)
                {
                    view.SetInput("Next question");
                    File.WriteAllText(lateFile, "Later");
                    var addFile = typeof(AiChatView).GetMethod("AddFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    var read = (Task)addFile.Invoke(view, [lateFile, attachmentTarget])!;
                    Assert.True(sink.PumpUntil(() => read.IsCompleted));
                    read.GetAwaiter().GetResult();
                }
                provider.Ready.SetResult();
                Assert.True(sink.PumpUntil(() => !module.Busy));
                Assert.Same(sent, Assert.Single(chat.Messages.First().Attachments));
                Assert.Equal(editWhileSending ? "Next question" : "", input.Text);
                Assert.Equal(editWhileSending ? 1 : 0, chips.Children.Count);
                module.Delete(chat);
                Assert.Empty(store.All);
                Assert.Empty(input.Text);
                Assert.Empty(chips.Children.Cast<UIElement>());
            });
        }
        finally { File.Delete(lateFile); File.Delete(file); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeletedConversationIsNotRecreatedWhenGenerationFinishes(bool cancel)
    {
        var file = Path.Combine(Path.GetTempPath(), "skerry-ai-race-" + Guid.NewGuid() + ".json");
        try
        {
            FakeSink.Run(sink =>
            {
                var store = new ChatStore(file);
                var chat = store.New();
                var provider = new DelayedProvider();
                var module = new AiModule(null!, _ => provider, store);
                SetField(module, "_current", chat);
                var run = module.Send("Question", []);
                Assert.True(module.Busy);
                store.Delete(chat.Id);
                if (cancel) module.Cancel(); else provider.Ready.SetResult();
                Assert.True(sink.PumpUntil(() => run.IsCompleted));
                run.GetAwaiter().GetResult();
                Assert.Empty(store.All);
                Assert.Empty(new ChatStore(file).All);
            });
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void SendAvailabilityTracksDraftFilesAndAnActiveStop()
    {
        using var dir = new TempDir();
        var attachment = Path.Combine(dir.Path, "draft.txt");
        File.WriteAllText(attachment, "Offline attachment");
        FakeSink.Run(sink =>
        {
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(sink.Ui));
            var store = new ChatStore(Path.Combine(dir.Path, "chats.json"));
            var chat = store.New();
            var other = store.New();
            var provider = new DelayedProvider(partial: true);
            var module = new AiModule(null!, _ => provider, store);
            var view = new AiChatView(module);
            SetField(module, "_view", view);
            SetField(module, "_current", chat);
            view.ShowChat(chat);
            var send = Field<Button>(view, "_send");
            Assert.False(send.IsEnabled);
            view.SetInput("Question");
            Assert.True(send.IsEnabled);
            view.SetInput(" \t\n ");
            Assert.False(send.IsEnabled);

            var addFile = typeof(AiChatView).GetMethod("AddFile", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var read = (Task)addFile.Invoke(view, [attachment, Field<object>(view, "_draft")])!;
            view.ShowChat(other);
            Assert.True(sink.PumpUntil(() => read.IsCompleted));
            read.GetAwaiter().GetResult();
            Assert.False(send.IsEnabled);
            view.ShowChat(chat);
            Assert.True(send.IsEnabled);
            var chips = Field<WrapPanel>(view, "_chips");
            var chip = (StackPanel)((Border)chips.Children[0]).Child;
            Assert.Single(chip.Children.OfType<Button>()).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(send.IsEnabled);

            view.AddPending(new ChatAttachment { Kind = AttachmentKind.Text, Name = "only.txt", Text = "Attached" });
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Empty(chips.Children.Cast<UIElement>());
            Assert.True(module.Busy);
            Assert.True(send.IsEnabled);
            Assert.Equal(Skerry.Core.Strings.T("ai.stop"), send.ToolTip);
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(sink.PumpUntil(() => !module.Busy));
            Assert.True(chat.Messages[^1].Stopped);
            Assert.False(send.IsEnabled);

            var run = module.StreamInto(other, new ChatMessage { Role = "assistant" });
            Assert.True(send.IsEnabled);
            provider.Ready.SetResult();
            Assert.True(sink.PumpUntil(() => run.IsCompleted));
            run.GetAwaiter().GetResult();
            Assert.False(send.IsEnabled);
        });
    }

    [Fact]
    public void ComposerRestoresEachChatsTextAndAttachmentsAndHistoryIncludesAllSavedChats()
    {
        var file = Path.Combine(Path.GetTempPath(), "skerry-ai-drafts-" + Guid.NewGuid() + ".json");
        try
        {
            FakeSink.Run(_ =>
            {
                var store = new ChatStore(file);
                var chats = Enumerable.Range(0, ChatStore.MaxChats).Select(_ => store.New()).ToArray();
                var module = new AiModule(null!, _ => new DelayedProvider(), store);
                var view = new AiChatView(module);
                view.ShowChat(chats[0]);
                view.SetInput("Draft one");
                var attachment = new ChatAttachment { Name = "one.txt", Kind = AttachmentKind.Text, Text = "One" };
                view.AddPending(attachment);
                view.ShowChat(chats[1]);
                Assert.Empty(Field<TextBox>(view, "_input").Text);
                Assert.Empty(Field<WrapPanel>(view, "_chips").Children.Cast<UIElement>());
                view.SetInput("Draft two");
                view.ShowChat(chats[0]);
                Assert.Equal("Draft one", Field<TextBox>(view, "_input").Text);
                Assert.Single(Field<WrapPanel>(view, "_chips").Children.Cast<UIElement>());
                view.ShowChat(chats[1]);
                Assert.Equal("Draft two", Field<TextBox>(view, "_input").Text);
                view.ShowChatList();
                var overlay = (Grid)Field<Border>(view, "_overlay").Child;
                var history = (StackPanel)overlay.Children.OfType<ScrollViewer>().Single().Content;
                Assert.Equal(ChatStore.MaxChats, history.Children.Count);
            });
        }
        finally { File.Delete(file); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ComposerStaysEditableUntilResponseAndSurvivesLoginFailure(bool fail, bool switchChat)
    {
        var file = Path.Combine(Path.GetTempPath(), "skerry-ai-send-" + Guid.NewGuid() + ".json");
        try
        {
            FakeSink.Run(sink =>
            {
                var store = new ChatStore(file);
                var chat = store.New();
                var provider = new DelayedProvider(fail);
                var module = new AiModule(null!, _ => provider, store);
                var view = new AiChatView(module);
                SetField(module, "_view", view);
                SetField(module, "_current", chat);
                view.ShowChat(chat);
                view.SetInput("Editable question");
                view.AddPending(new ChatAttachment { Name = "draft.txt", Kind = AttachmentKind.Text, Text = "Draft" });
                Field<Button>(view, "_send").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.True(module.Busy);
                Assert.Equal("Editable question", Field<TextBox>(view, "_input").Text);
                var messages = Field<StackPanel>(view, "_messages");
                var waiting = (StackPanel)((Border)messages.Children[1]).Child;
                Assert.Single(waiting.Children.OfType<TextBlock>());
                if (switchChat)
                {
                    var other = store.New();
                    SetField(module, "_current", other);
                    view.ShowChat(other);
                    view.SetInput("Other draft");
                }
                provider.Ready.SetResult();
                Assert.True(sink.PumpUntil(() => !module.Busy));
                if (switchChat)
                {
                    Assert.Equal("Other draft", Field<TextBox>(view, "_input").Text);
                    view.ShowChat(chat);
                }
                Assert.Equal(fail ? "Editable question" : "", Field<TextBox>(view, "_input").Text);
                Assert.Equal(fail ? 1 : 0, Field<WrapPanel>(view, "_chips").Children.Count);
                Assert.Equal(fail ? 0 : 1, chat.Messages.Count(m => m.Role == "user"));
            });
        }
        finally { File.Delete(file); }
    }
}
