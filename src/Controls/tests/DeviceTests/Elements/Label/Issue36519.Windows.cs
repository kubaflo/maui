#if WINDOWS
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36519")]
	public class Issue36519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingFormattedTextClearsPreviousTextHighlighter()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var affectedLabel = new Label
			{
				AutomationId = "AffectedLabel",
				FormattedText = CreateInitialText(),
			};
			var updateButton = new Button
			{
				AutomationId = "UpdateButton",
				Text = "Replace formatted text",
			};
			var contextLabel = new Label
			{
				AutomationId = "ContextLabel",
				Text = "FormattedText replacement",
			};
			var headingLabel = new Label
			{
				Text = "Issue 36519: replace the highlighted FormattedText",
			};
			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
				RowSpacing = 16,
			};

			grid.Add(headingLabel);
			grid.Add(affectedLabel);
			grid.Add(updateButton);
			grid.Add(contextLabel);
			Grid.SetRow(affectedLabel, 1);
			Grid.SetRow(updateButton, 2);
			Grid.SetRow(contextLabel, 3);

			var page = new ContentPage
			{
				Content = grid,
			};
			var callbackCount = 0;
			updateButton.Clicked += (_, _) =>
			{
				callbackCount++;
				affectedLabel.FormattedText = CreateUpdatedText();
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				WTextBlock platformLabel = labelHandler.PlatformView;

				Assert.True(
					platformLabel.Text == "OLD reference text",
					$"Expected initial native text 'OLD reference text', observed '{platformLabel.Text}'.");
				Assert.True(
					platformLabel.TextHighlighters.Count == 1,
					$"Expected one initial TextHighlighter, observed {platformLabel.TextHighlighters.Count}.");

				var initialHighlighter = platformLabel.TextHighlighters[0];
				Assert.True(
					initialHighlighter.Ranges.Count == 1,
					$"Expected one initial highlighted range, observed {initialHighlighter.Ranges.Count}.");
				var initialRange = initialHighlighter.Ranges[0];
				Assert.True(
					initialRange.StartIndex == 0 && initialRange.Length == 3,
					$"Expected initial range start 0 length 3, observed start {initialRange.StartIndex} length {initialRange.Length}.");

				var buttonHandler = Assert.IsType<ButtonHandler>(updateButton.Handler);
				WButton platformButton = buttonHandler.PlatformView;
				var automationPeer = new ButtonAutomationPeer(platformButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();

				await AssertionExtensions.AssertEventually(
					() => callbackCount == 1 &&
						platformLabel.Text == "New text TARGET" &&
						platformLabel.TextHighlighters.Any(highlighter =>
							highlighter.Ranges.Any(range => range.StartIndex == 9 && range.Length == 6)),
					message: "The button callback, replacement native text, and TARGET range did not all appear.");

				Assert.True(
					callbackCount == 1,
					$"Expected exactly one button callback, observed {callbackCount}.");
				Assert.True(
					platformLabel.Text == "New text TARGET",
					$"Expected replacement native text 'New text TARGET', observed '{platformLabel.Text}'.");

				var targetRange = platformLabel.TextHighlighters
					.SelectMany(highlighter => highlighter.Ranges)
					.First(range => range.StartIndex == 9 && range.Length == 6);
				Assert.True(
					targetRange.StartIndex == 9 && targetRange.Length == 6,
					$"Expected TARGET range start 9 length 6, observed start {targetRange.StartIndex} length {targetRange.Length}.");

				var observedCount = -1;
				observedCount = platformLabel.TextHighlighters.Count;
				Assert.True(
					observedCount == 1,
					$"FormattedText replacement retained stale Windows TextHighlighters: expected 1, observed {observedCount}.");
			});
		}

		static FormattedString CreateInitialText()
		{
			var formattedText = new FormattedString();
			formattedText.Spans.Add(new Span
			{
				Text = "OLD",
				BackgroundColor = Colors.Gold,
			});
			formattedText.Spans.Add(new Span { Text = " reference text" });
			return formattedText;
		}

		static FormattedString CreateUpdatedText()
		{
			var formattedText = new FormattedString();
			formattedText.Spans.Add(new Span { Text = "New text " });
			formattedText.Spans.Add(new Span
			{
				Text = "TARGET",
				BackgroundColor = Colors.Lime,
			});
			return formattedText;
		}
	}
}
#endif

