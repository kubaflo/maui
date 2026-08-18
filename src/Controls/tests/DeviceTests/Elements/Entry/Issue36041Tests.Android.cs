#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Entry)]
	public class Issue36041 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ZeroNativePaddingRemovesEntryFontPadding()
		{
			Assert.True(OperatingSystem.IsAndroid());

			var layoutCompletions = new Dictionary<Entry, TaskCompletionSource<int>>();

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Entry>(_ =>
						new Issue36041EntryHandler().Initialize(entry => layoutCompletions[entry]));
				});
			});

			var scenarioLayout = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 16,
				Children =
				{
					new Label { Text = "Entry underline spacing", FontSize = 24 },
					new Label { Text = "NO BUG:" },
					new Button { Text = "Reset scenario" },
					new Button { Text = "Create affected Entry" },
					new Button { Text = "Check spacing", IsVisible = false },
				}
			};
			var page = new ContentPage { Content = scenarioLayout };
			var firstEntry = CreateEntry();
			layoutCompletions[firstEntry] =
				new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
			scenarioLayout.Children.Insert(1, firstEntry);

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				var firstHandler = Assert.IsType<Issue36041EntryHandler>(firstEntry.Handler);
				var firstPlatformView = firstHandler.PlatformView;
				await firstHandler.LayoutTask.WaitAsync(TimeSpan.FromSeconds(5));
				int firstLayoutWidth = firstHandler.ObservedLayoutWidth;
				Assert.NotEqual(-1, firstLayoutWidth);
				AssertEntryState(firstEntry, scenarioLayout, firstHandler, firstLayoutWidth);
				int firstPaddingBottom = firstPlatformView.PaddingBottom;
				bool firstIncludeFontPadding = firstPlatformView.IncludeFontPadding;

				scenarioLayout.Children.Remove(firstEntry);
				Assert.Null(firstEntry.Parent);
				await AssertEventually(
					() => !firstPlatformView.IsAttachedToWindow,
					message: "The first Entry native view did not detach after removal.");

				var secondEntry = CreateEntry();
				layoutCompletions[secondEntry] =
					new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
				scenarioLayout.Children.Insert(1, secondEntry);
				var secondHandler = Assert.IsType<Issue36041EntryHandler>(secondEntry.Handler);
				await secondHandler.LayoutTask.WaitAsync(TimeSpan.FromSeconds(5));
				int secondLayoutWidth = secondHandler.ObservedLayoutWidth;
				Assert.NotEqual(-1, secondLayoutWidth);
				AssertEntryState(secondEntry, scenarioLayout, secondHandler, secondLayoutWidth);
				int secondPaddingBottom = secondHandler.PlatformView.PaddingBottom;
				bool secondIncludeFontPadding = secondHandler.PlatformView.IncludeFontPadding;

				Assert.False(firstIncludeFontPadding,
					$"Issue36041 Entry retained Android font padding after zero native padding: PaddingBottom={firstPaddingBottom}, IncludeFontPadding={firstIncludeFontPadding}; expected PaddingBottom=0 and IncludeFontPadding=False.");
				Assert.False(secondIncludeFontPadding,
					$"Issue36041 Entry retained Android font padding after zero native padding: PaddingBottom={secondPaddingBottom}, IncludeFontPadding={secondIncludeFontPadding}; expected PaddingBottom=0 and IncludeFontPadding=False.");
			});
		}

		static Entry CreateEntry() =>
			new Entry
			{
				Placeholder = "Enter text here",
				Text = "9x9",
			};

		static void AssertEntryState(
			Entry entry,
			VerticalStackLayout scenarioLayout,
			Issue36041EntryHandler handler,
			int observedLayoutWidth)
		{
			Assert.True(observedLayoutWidth > 0);
			Assert.Same(entry, scenarioLayout.Children[1]);
			Assert.Equal("9x9", entry.Text);
			Assert.Equal("Enter text here", entry.Placeholder);
			Assert.Null(entry.Style);
			Assert.False(entry.IsSet(VisualElement.BackgroundProperty));
			Assert.NotNull(handler.PlatformView.Background);
			Assert.True(handler.PlatformView.IsAttachedToWindow);
			Assert.Equal(0, handler.PlatformView.PaddingLeft);
			Assert.Equal(0, handler.PlatformView.PaddingTop);
			Assert.Equal(0, handler.PlatformView.PaddingRight);
			Assert.Equal(0, handler.PlatformView.PaddingBottom);
		}

		public sealed class Issue36041EntryHandler : EntryHandler
		{
			Func<Entry, TaskCompletionSource<int>> _getLayoutCompletion;
			TaskCompletionSource<int> _layoutCompletion;

			public Issue36041EntryHandler Initialize(Func<Entry, TaskCompletionSource<int>> getLayoutCompletion)
			{
				_getLayoutCompletion = getLayoutCompletion;
				return this;
			}

			public Task<int> LayoutTask => _layoutCompletion.Task;

			public int ObservedLayoutWidth { get; private set; } = -1;

			protected override void ConnectHandler(Microsoft.Maui.Platform.MauiAppCompatEditText platformView)
			{
				base.ConnectHandler(platformView);
				_layoutCompletion = _getLayoutCompletion((Entry)VirtualView);
				platformView.SetPadding(0, 0, 0, 0);
				platformView.ShowSoftInputOnFocus = false;
				platformView.LayoutChange += OnLayoutChanged;
			}

			protected override void DisconnectHandler(Microsoft.Maui.Platform.MauiAppCompatEditText platformView)
			{
				platformView.LayoutChange -= OnLayoutChanged;
				base.DisconnectHandler(platformView);
			}

			void OnLayoutChanged(object sender, global::Android.Views.View.LayoutChangeEventArgs e)
			{
				int width = e.Right - e.Left;
				if (width > 0)
				{
					ObservedLayoutWidth = width;
					_layoutCompletion.TrySetResult(width);
				}
			}
		}
	}
}
#endif
