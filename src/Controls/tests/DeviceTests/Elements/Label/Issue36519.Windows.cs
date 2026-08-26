#if WINDOWS
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36519")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingFormattedTextReplacesNativeTextHighlighters()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var affectedLabel = new Label
			{
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = "OLD " },
						new Span { Text = "HIGHLIGHT", BackgroundColor = Colors.Yellow },
						new Span { Text = " HERE" },
					},
				},
			};

			var updateButton = new Button { Text = "Replace FormattedText" };
			var buttonClicked = false;
			updateButton.Clicked += (_, _) =>
			{
				affectedLabel.FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = "NEW MARKER " },
						new Span { Text = "AREA", BackgroundColor = Colors.Lime },
					},
				};
				buttonClicked = true;
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "The highlighted text below starts with one yellow range. Replace the FormattedText to move the highlight to AREA." },
					affectedLabel,
					new Label { Text = "Active highlighters before update: pending" },
					new Label { Text = "Replacement status:" },
					updateButton,
				},
			};

			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				WTextBlock textBlock = labelHandler.PlatformView;
				Assert.NotNull(textBlock);
				Assert.Equal("OLD HIGHLIGHT HERE", textBlock.Text);

				var initialHighlighter = Assert.Single(textBlock.TextHighlighters);
				var initialRange = Assert.Single(initialHighlighter.Ranges);
				Assert.Equal(4, initialRange.StartIndex);
				Assert.Equal(9, initialRange.Length);

				var formattedTextChanged = false;
				var updatedHighlighterCount = -1;
				affectedLabel.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == Label.FormattedTextProperty.PropertyName)
						formattedTextChanged = true;
				};

				var buttonHandler = Assert.IsType<ButtonHandler>(updateButton.Handler);
				var automationPeer = new ButtonAutomationPeer(buttonHandler.PlatformView);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();

				var clickObserved = await AssertHelpers.Wait(() => buttonClicked);
				Assert.True(clickObserved, "The attached replacement button should raise Clicked.");

				Assert.True(formattedTextChanged, "FormattedText property-change callback should occur after the button click.");

				var nativeTextUpdated = await AssertHelpers.Wait(() => textBlock.Text == "NEW MARKER AREA");
				Assert.True(nativeTextUpdated, "Native TextBlock text should update after FormattedText replacement.");

				Assert.Equal(2, affectedLabel.FormattedText.Spans.Count);
				Assert.Equal("AREA", affectedLabel.FormattedText.Spans[1].Text);
				Assert.Equal(Colors.Lime, affectedLabel.FormattedText.Spans[1].BackgroundColor);
				Assert.Equal("NEW MARKER AREA", textBlock.Text);

				updatedHighlighterCount = textBlock.TextHighlighters.Count;
				var observedRanges = string.Join(", ",
					textBlock.TextHighlighters
						.SelectMany(highlighter => highlighter.Ranges)
						.Select(range => $"[{range.StartIndex},{range.Length}]"));

				Assert.True(updatedHighlighterCount == 1,
					$"Replacing FormattedText should leave exactly one native TextHighlighter. Expected count: 1; observed count: {updatedHighlighterCount}; expected ranges: [11,4]; observed ranges: {observedRanges}");

				var updatedRange = Assert.Single(Assert.Single(textBlock.TextHighlighters).Ranges);
				Assert.Equal(11, updatedRange.StartIndex);
				Assert.Equal(4, updatedRange.Length);
			});
		}
	}
}
#endif

