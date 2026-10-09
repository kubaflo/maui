#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26635")]
	public class Issue26635 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ContentPageArrangeOverrideRunsDuringInitialLayout()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<TrackingContentPage, PageHandler>();
					handlers.AddHandler<TrackingContentView, ContentViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var titleLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 22,
				Text = "ContentPage and ContentView ArrangeOverride callbacks"
			};
			var pageCountLabel = new Label
			{
				FontSize = 18,
				Text = "ContentPage ArrangeOverride count: waiting"
			};
			var viewCountLabel = new Label
			{
				FontSize = 18,
				Text = "ContentView ArrangeOverride count: waiting"
			};
			var detailsLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "Initial layout callback details"
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					titleLabel,
					pageCountLabel,
					viewCountLabel,
					detailsLabel
				}
			};
			var contentArrangeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var pageArrangeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var contentView = new TrackingContentView
			{
				ArrangeCompletion = contentArrangeCompletion,
				ArrangeCount = -1,
				Content = layout
			};
			var page = new TrackingContentPage
			{
				ArrangeCompletion = pageArrangeCompletion,
				ArrangeCount = -1,
				Title = "ArrangeOverride page probe",
				Content = contentView
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async handler =>
			{
				Assert.NotNull(handler.PlatformView);
				Assert.Same(contentView, page.Content);
				Assert.Same(layout, contentView.Content);
				Assert.Collection(
					layout.Children,
					child => Assert.Same(titleLabel, child),
					child => Assert.Same(pageCountLabel, child),
					child => Assert.Same(viewCountLabel, child),
					child => Assert.Same(detailsLabel, child));

				Assert.NotNull(page.Handler);
				Assert.NotNull(contentView.Handler);
				var pageNativeView = Assert.IsAssignableFrom<UIView>(page.Handler.PlatformView);
				var contentNativeView = Assert.IsAssignableFrom<UIView>(contentView.Handler.PlatformView);
				Assert.NotNull(pageNativeView.Window);
				Assert.NotNull(contentNativeView.Window);
				Assert.Same(pageNativeView.Window, contentNativeView.Window);
				Assert.True(contentNativeView.Frame.Width > 0);
				Assert.True(contentNativeView.Frame.Height > 0);

				var contentCallbackObserved = await AssertHelpers.Wait(
					() => contentArrangeCompletion.Task.IsCompleted,
					timeout: 2000);
				Assert.True(
					contentCallbackObserved && contentView.ArrangeCount >= 1,
					$"ContentView ArrangeOverride was not called. ContentView count: {contentView.ArrangeCount}.");

				var pageCallbackObserved = await AssertHelpers.Wait(
					() => pageArrangeCompletion.Task.IsCompleted,
					timeout: 2000);
				Assert.True(
					pageCallbackObserved && page.ArrangeCount >= 1,
					$"ContentPage ArrangeOverride was not called after direct ContentView layout. Page count: {page.ArrangeCount}; ContentView count: {contentView.ArrangeCount}.");
			});
		}

		sealed class TrackingContentPage : ContentPage
		{
			public TaskCompletionSource ArrangeCompletion { get; set; }

			public int ArrangeCount { get; set; }

			protected override Size ArrangeOverride(Rect bounds)
			{
				ArrangeCount = Math.Max(1, ArrangeCount + 1);
				ArrangeCompletion.TrySetResult();
				return base.ArrangeOverride(bounds);
			}
		}

		sealed class TrackingContentView : ContentView
		{
			public TaskCompletionSource ArrangeCompletion { get; set; }

			public int ArrangeCount { get; set; }

			protected override Size ArrangeOverride(Rect bounds)
			{
				var arrangedSize = base.ArrangeOverride(bounds);
				ArrangeCount = Math.Max(1, ArrangeCount + 1);
				ArrangeCompletion.TrySetResult();
				return arrangedSize;
			}
		}
	}
}
#endif

