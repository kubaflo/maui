using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

#if IOS && !MACCATALYST
namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27866")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue27866 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlListsRenderNativeMarkers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var unorderedLabel = new Label
			{
				TextType = TextType.Html
			};

			var orderedLabel = new Label
			{
				TextType = TextType.Html
			};

			var readyLabel = new Label
			{
				Text = "Preparing"
			};

			var resultLabel = new Label
			{
				Text = "Result pending"
			};

			var checkButton = new Button
			{
				IsEnabled = false,
				Text = "Check rendered lists"
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			stack.Add(unorderedLabel);
			stack.Add(orderedLabel);
			stack.Add(readyLabel);
			stack.Add(resultLabel);
			stack.Add(checkButton);

			var page = new ContentPage
			{
				Title = "Home",
				Content = stack
			};

			bool pageLoaded = false;
			page.Loaded += (_, _) => pageLoaded = true;

			unorderedLabel.Text = "<ul><li>item 1</li><li>item 2</li><li>item 3</li></ul>";
			orderedLabel.Text = "<ol><li>item 1</li><li>item 2</li><li>item 3</li></ol>";

			string unorderedText = null;
			string orderedText = null;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.True(pageLoaded, "The page should complete its Loaded transition.");
				Assert.NotNull(page.Window);
				Assert.NotNull(unorderedLabel.Handler);
				Assert.NotNull(orderedLabel.Handler);

				var unorderedPlatformLabel = Assert.IsAssignableFrom<UILabel>(unorderedLabel.Handler.PlatformView);
				var orderedPlatformLabel = Assert.IsAssignableFrom<UILabel>(orderedLabel.Handler.PlatformView);

				Assert.NotNull(unorderedPlatformLabel.Window);
				Assert.NotNull(orderedPlatformLabel.Window);

				await AssertionExtensions.AssertEventually(
					() =>
					{
						unorderedText = unorderedPlatformLabel.AttributedText?.Value;
						return !string.IsNullOrEmpty(unorderedText);
					},
					timeout: 5000,
					message: "The unordered list did not produce native attributed text.");

				await AssertionExtensions.AssertEventually(
					() =>
					{
						orderedText = orderedPlatformLabel.AttributedText?.Value;
						return !string.IsNullOrEmpty(orderedText);
					},
					timeout: 5000,
					message: "The ordered list did not produce native attributed text.");
			});

			Assert.NotNull(unorderedText);
			Assert.NotNull(orderedText);
			AssertContainsEveryItem(unorderedText);
			AssertContainsEveryItem(orderedText);

			Assert.True(
				HasMarkerBeforeEveryItem(unorderedText),
				$"Unordered HTML list marker missing from native attributed text. Native text: '{unorderedText}'");
			Assert.True(
				HasMarkerBeforeEveryItem(orderedText),
				$"Ordered HTML list marker missing from native attributed text. Native text: '{orderedText}'");
		}

		static void AssertContainsEveryItem(string renderedText)
		{
			Assert.Contains("item 1", renderedText, StringComparison.Ordinal);
			Assert.Contains("item 2", renderedText, StringComparison.Ordinal);
			Assert.Contains("item 3", renderedText, StringComparison.Ordinal);
		}

		static bool HasMarkerBeforeEveryItem(string renderedText)
		{
			int previousItemEnd = 0;

			for (int itemNumber = 1; itemNumber <= 3; itemNumber++)
			{
				string item = $"item {itemNumber}";
				int itemIndex = renderedText.IndexOf(item, previousItemEnd, StringComparison.Ordinal);

				if (itemIndex < 0 ||
					string.IsNullOrWhiteSpace(renderedText.Substring(previousItemEnd, itemIndex - previousItemEnd)))
				{
					return false;
				}

				previousItemEnd = itemIndex + item.Length;
			}

			return true;
		}
	}
}
#endif

