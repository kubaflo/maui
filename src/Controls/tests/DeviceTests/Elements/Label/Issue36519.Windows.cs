#if WINDOWS
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36519")]
	public class Issue36519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingFormattedTextClearsPreviousTextHighlighters()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			const string initialHighlightedText = "OLD HIGHLIGHT";
			const string initialSuffix = " reference";
			const string replacementPrefix = "new text: ";
			const string replacementHighlightedText = "TARGET";
			const string replacementSuffix = " only";
			const string replacementText = replacementPrefix + replacementHighlightedText + replacementSuffix;

			var affectedLabel = new Label
			{
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span
						{
							Text = initialHighlightedText,
							BackgroundColor = Colors.Yellow
						},
						new Span { Text = initialSuffix }
					}
				}
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "The yellow highlight should disappear after the update; only TARGET should be blue." },
					affectedLabel,
					new Button { Text = "Update formatted text" }
				}
			};
			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				var nativeLabel = Assert.IsType<WTextBlock>(labelHandler.PlatformView);

				Assert.Equal(initialHighlightedText + initialSuffix, GetInlineText(nativeLabel));
				Assert.Equal(1, nativeLabel.TextHighlighters.Count);

				var initialHighlighter = nativeLabel.TextHighlighters[0];
				Assert.Equal(1, initialHighlighter.Ranges.Count);
				Assert.Equal(0, initialHighlighter.Ranges[0].StartIndex);
				Assert.Equal(initialHighlightedText.Length, initialHighlighter.Ranges[0].Length);
				Assert.Equal(Colors.Yellow, Assert.IsType<WSolidColorBrush>(initialHighlighter.Background).Color.ToColor());

				var layoutUpdatedCount = 0;
				nativeLabel.LayoutUpdated += OnLayoutUpdated;

				affectedLabel.FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = replacementPrefix },
						new Span
						{
							Text = replacementHighlightedText,
							BackgroundColor = Colors.Blue,
							TextColor = Colors.White
						},
						new Span { Text = replacementSuffix }
					}
				};

				var observedText = "<not updated>";
				await AssertionExtensions.AssertEventually(
					() =>
					{
						observedText = GetInlineText(nativeLabel);
						return observedText == replacementText;
					},
					message: "Issue36519 native inline text did not reflect the FormattedText replacement.");
				Assert.Equal(replacementText, observedText);

				await AssertionExtensions.AssertEventually(
					() => layoutUpdatedCount > 0,
					message: "Issue36519 native label did not complete a layout after the FormattedText replacement.");
				Assert.True(layoutUpdatedCount > 0);
				nativeLabel.LayoutUpdated -= OnLayoutUpdated;

				var hasExpectedReplacementHighlighter = nativeLabel.TextHighlighters.Count == 1;
				if (hasExpectedReplacementHighlighter)
				{
					var replacementHighlighter = nativeLabel.TextHighlighters[0];
					hasExpectedReplacementHighlighter =
						replacementHighlighter.Ranges.Count == 1 &&
						replacementHighlighter.Ranges[0].StartIndex == replacementPrefix.Length &&
						replacementHighlighter.Ranges[0].Length == replacementHighlightedText.Length &&
						replacementHighlighter.Background is WSolidColorBrush background &&
						background.Color.ToColor() == Colors.Blue &&
						replacementHighlighter.Foreground is WSolidColorBrush foreground &&
						foreground.Color.ToColor() == Colors.White;
				}

				Assert.True(
					hasExpectedReplacementHighlighter,
					$"Issue36519 stale TextHighlighters remained after FormattedText replacement: observed count {nativeLabel.TextHighlighters.Count}, ranges {GetHighlighterRanges(nativeLabel)}; expected count 1, range {replacementPrefix.Length}:{replacementHighlightedText.Length}, background Blue, foreground White.");

				void OnLayoutUpdated(object sender, object args)
				{
					layoutUpdatedCount++;
				}
			});
		}

		static string GetInlineText(WTextBlock textBlock)
		{
			var text = new StringBuilder();
			for (var i = 0; i < textBlock.Inlines.Count; i++)
			{
				if (textBlock.Inlines[i] is WRun run)
					text.Append(run.Text);
			}

			return text.ToString();
		}

		static string GetHighlighterRanges(WTextBlock textBlock)
		{
			var ranges = new StringBuilder();
			for (var highlighterIndex = 0; highlighterIndex < textBlock.TextHighlighters.Count; highlighterIndex++)
			{
				var highlighter = textBlock.TextHighlighters[highlighterIndex];
				for (var rangeIndex = 0; rangeIndex < highlighter.Ranges.Count; rangeIndex++)
				{
					if (ranges.Length > 0)
						ranges.Append(", ");

					var range = highlighter.Ranges[rangeIndex];
					ranges.Append(range.StartIndex).Append(':').Append(range.Length);
				}
			}

			return ranges.Length == 0 ? "<none>" : ranges.ToString();
		}
	}
}
#endif

