using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36519")]
	public class Issue36519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingFormattedTextRemovesPreviousTextHighlighter()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var affectedLabel = new Label
			{
				FontSize = 28,
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span
						{
							Text = "OLD",
							BackgroundColor = Colors.Gold
						},
						new Span { Text = " highlight should disappear after update." }
					}
				}
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				Children = { affectedLabel }
			};

			await AttachAndRun<LayoutHandler>(layout, async _ =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedLabel.Handler);
				var platformLabel = labelHandler.PlatformView;

				Assert.Equal("OLD highlight should disappear after update.", platformLabel.Text);
				Assert.Single(platformLabel.TextHighlighters);

				var formattedTextChanged = false;
				var observedHighlighterCount = -1;
				affectedLabel.PropertyChanged += (_, args) =>
					formattedTextChanged |= args.PropertyName == nameof(Label.FormattedText);

				affectedLabel.FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = "Updated text with " },
						new Span
						{
							Text = "NEW",
							BackgroundColor = Colors.LightGreen
						},
						new Span { Text = " highlighted." }
					}
				};

				await AssertEventually(
					() => formattedTextChanged,
					message: "Label.FormattedText should report its replacement.");
				Assert.True(formattedTextChanged);

				await AssertEventually(
					() =>
					{
						if (platformLabel.Text != "Updated text with NEW highlighted.")
							return false;

						observedHighlighterCount = platformLabel.TextHighlighters.Count;
						return true;
					},
					message: "The native TextBlock should contain the updated formatted text.");

				Assert.Equal("Updated text with NEW highlighted.", platformLabel.Text);
				Assert.True(
					observedHighlighterCount == 1,
					"Windows Label should have exactly one TextHighlighter after FormattedText is replaced.");

				var remainingHighlighter = Assert.Single(platformLabel.TextHighlighters);
				var highlightedRange = Assert.Single(remainingHighlighter.Ranges);
				Assert.Equal(18, highlightedRange.StartIndex);
				Assert.Equal(3, highlightedRange.Length);
			});
		}
	}
}
