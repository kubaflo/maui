using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
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
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			const string initialHighlightedText = "FIRST";
			const string initialRemainingText = " remains highlighted before replacement.";
			const string replacementPrefix = "Updated text moves the highlight to the ";
			const string replacementHighlightedText = "END";
			var initialText = initialHighlightedText + initialRemainingText;
			var replacementText = replacementPrefix + replacementHighlightedText;

			var initialFormattedText = new FormattedString();
			initialFormattedText.Spans.Add(new Span
			{
				Text = initialHighlightedText,
				BackgroundColor = Colors.Yellow
			});
			initialFormattedText.Spans.Add(new Span { Text = initialRemainingText });

			var affectedLabel = new Label { FormattedText = initialFormattedText };
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label { Text = "Windows FormattedText replacement", FontSize = 24 },
							new Label { Text = "The affected Label stays visible before and after its FormattedText is replaced." },
							affectedLabel,
							new Label { Text = "Native highlighters before: pending" },
							new Button { Text = "Replace FormattedText" },
							new Label { Text = "Replacement not applied" }
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var labelHandler = affectedLabel.Handler as LabelHandler;
				Assert.NotNull(labelHandler);
				WTextBlock platformView = labelHandler.PlatformView;

				Assert.Equal(initialText, platformView.Text);
				Assert.Equal(Colors.Yellow, initialFormattedText.Spans[0].BackgroundColor);
				var initialHighlighter = Assert.Single(platformView.TextHighlighters);
				var initialRange = Assert.Single(initialHighlighter.Ranges);
				Assert.Equal(0, initialRange.StartIndex);
				Assert.Equal(initialHighlightedText.Length, initialRange.Length);

				var replacementFormattedText = new FormattedString();
				replacementFormattedText.Spans.Add(new Span { Text = replacementPrefix });
				replacementFormattedText.Spans.Add(new Span
				{
					Text = replacementHighlightedText,
					BackgroundColor = Colors.Lime
				});

				string postTriggerText = null;
				var postTriggerCount = -1;
				affectedLabel.FormattedText = replacementFormattedText;

				await AssertHelpers.AssertEventually(() =>
				{
					postTriggerText = platformView.Text;
					postTriggerCount = platformView.TextHighlighters.Count;
					return postTriggerText == replacementText;
				}, message: "The native TextBlock did not display the replacement FormattedText.");

				Assert.NotNull(postTriggerText);
				Assert.Equal(replacementText, postTriggerText);
				Assert.NotEqual(-1, postTriggerCount);
				Assert.Same(replacementFormattedText, affectedLabel.FormattedText);
				Assert.Equal(2, replacementFormattedText.Spans.Count);
				Assert.Equal(Colors.Lime, replacementFormattedText.Spans[1].BackgroundColor);
				Assert.Same(labelHandler, affectedLabel.Handler);
				Assert.Same(platformView, labelHandler.PlatformView);

				var expectedStart = replacementPrefix.Length;
				var expectedLength = replacementHighlightedText.Length;
				Assert.Contains(platformView.TextHighlighters,
					highlighter => highlighter.Ranges.Any(
						range => range.StartIndex == expectedStart && range.Length == expectedLength));

				var observedRanges = string.Join(", ",
					platformView.TextHighlighters.SelectMany(
						highlighter => highlighter.Ranges.Select(
							range => $"[{range.StartIndex},{range.Length}]")));
				var hasOnlyReplacementRange =
					postTriggerCount == 1 &&
					platformView.TextHighlighters[0].Ranges.Count == 1 &&
					platformView.TextHighlighters[0].Ranges[0].StartIndex == expectedStart &&
					platformView.TextHighlighters[0].Ranges[0].Length == expectedLength;

				Assert.True(
					hasOnlyReplacementRange,
					$"Replacing FormattedText should leave exactly one native TextHighlighter; observed {postTriggerCount} with ranges {observedRanges}, expected 1 with range [{expectedStart},{expectedLength}].");
			});
		}
	}
}

