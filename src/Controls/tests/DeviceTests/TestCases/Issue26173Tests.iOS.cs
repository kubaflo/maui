#if IOS && !MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26173")]
	public class Issue26173 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SampleContentDoesNotIncludeRestrictedFonts()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			const string notEvaluated = "NOT_EVALUATED";
			var reportedFonts = new[]
			{
				"FluentSystemIcons-Regular.ttf",
				"SegoeUI-Semibold.ttf",
			};
			var clickCompleted = new TaskCompletionSource();
			var resultUpdated = new TaskCompletionSource();
			var resultLabel = new Label { Text = notEvaluated };
			var button = new Button { Text = "Check bundled sample fonts" };
			var segoeFontEntry = new Label { Text = "SegoeUI-Semibold.ttf" };
			var fluentFontEntry = new Label { Text = "FluentSystemIcons-Regular.ttf" };
			var fontEntryStack = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					segoeFontEntry,
					fluentFontEntry,
				},
			};
			var border = new Border
			{
				Padding = 16,
				Content = fontEntryStack,
			};
			var contentStack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					border,
					resultLabel,
					button,
				},
			};
			var scrollView = new ScrollView { Content = contentStack };
			var page = new ContentPage { Content = scrollView };

			string[] restrictedFonts = null;
			UIButton activatedNativeButton = null;
			bool reportedEntriesSelected = false;

			resultLabel.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == Label.TextProperty.PropertyName && resultLabel.Text != notEvaluated)
					resultUpdated.TrySetResult();
			};

			button.Clicked += (sender, _) =>
			{
				var activatedButton = Assert.IsType<Button>(sender);
				activatedNativeButton = Assert.IsType<UIButton>(activatedButton.Handler.PlatformView);

				var selectedFonts = fontEntryStack.Children
					.Cast<Label>()
					.Select(entry => Assert.IsAssignableFrom<UILabel>(entry.Handler.PlatformView).Text)
					.ToArray();
				reportedEntriesSelected = reportedFonts.All(selectedFonts.Contains);
				restrictedFonts = selectedFonts.Intersect(reportedFonts, StringComparer.Ordinal).ToArray();
				resultLabel.Text = string.Join(", ", selectedFonts);
				clickCompleted.TrySetResult();
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AssertPlatformViewCreated(page);
				AssertPlatformViewCreated(scrollView);
				AssertPlatformViewCreated(contentStack);
				AssertPlatformViewCreated(border);
				AssertPlatformViewCreated(fontEntryStack);
				AssertPlatformViewCreated(segoeFontEntry);
				AssertPlatformViewCreated(fluentFontEntry);
				AssertPlatformViewCreated(resultLabel);
				AssertPlatformViewCreated(button);
				Assert.Equal(notEvaluated, resultLabel.Text);

				var initialNativeButton = Assert.IsType<UIButton>(button.Handler.PlatformView);
				initialNativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await clickCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
				await resultUpdated.Task.WaitAsync(TimeSpan.FromSeconds(2));

				Assert.True(reportedEntriesSelected);
				Assert.Same(initialNativeButton, activatedNativeButton);
				Assert.NotNull(restrictedFonts);
				Assert.True(
					restrictedFonts.Length == 0,
					$"Issue 26173: generated sample content includes restricted font files: {string.Join(", ", restrictedFonts)}");
			});
		}

		static void AssertPlatformViewCreated(Element element)
		{
			Assert.NotNull(element.Handler);
			Assert.NotNull(element.Handler.PlatformView);
		}
	}
}
#endif

