#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using MWindow = Microsoft.Maui.Controls.Window;
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
					handlers.AddHandler<MWindow, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var issueLabel = new Label
			{
				AutomationId = "IssueLabel",
				FontSize = 24,
				FormattedText = CreateInitialText()
			};
			var page = new ContentPage
			{
				Title = "FormattedText highlighter update",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "The highlighted word must move from FIRST to END without leaving the old highlight behind."
						},
						issueLabel,
						new Button
						{
							AutomationId = "UpdateButton",
							Text = "Update FormattedText"
						},
						new Button
						{
							AutomationId = "CheckButton",
							Text = "Check retained highlighters"
						},
						new Label
						{
							AutomationId = "ResultLabel",
							Text = "Result"
						}
					}
				}
			};

			await AttachAndRun<PageHandler>(page, _ =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(issueLabel.Handler);
				var platformLabel = Assert.IsType<WTextBlock>(labelHandler.PlatformView);
				Assert.Equal("FIRST highlight is here", platformLabel.Text);

				var initialHighlighter = Assert.Single(platformLabel.TextHighlighters);
				var initialRange = Assert.Single(initialHighlighter.Ranges);
				Assert.Equal(0, initialRange.StartIndex);
				Assert.Equal(5, initialRange.Length);

				var formattedTextChanged = false;
				issueLabel.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == nameof(Label.FormattedText))
						formattedTextChanged = true;
				};

				issueLabel.FormattedText = CreateUpdatedText();

				Assert.True(formattedTextChanged);
				const string updatedText = "Updated text highlights END";
				Assert.Equal(updatedText, platformLabel.Text);

				Assert.Equal(1, platformLabel.TextHighlighters.Count);
				var updatedHighlighter = Assert.Single(platformLabel.TextHighlighters);
				var updatedRange = Assert.Single(updatedHighlighter.Ranges);
				Assert.Equal("Updated text highlights ".Length, updatedRange.StartIndex);
				Assert.Equal("END".Length, updatedRange.Length);
			});
		}

		static FormattedString CreateInitialText()
		{
			var formatted = new FormattedString();
			formatted.Spans.Add(new Span
			{
				Text = "FIRST",
				BackgroundColor = Colors.Gold
			});
			formatted.Spans.Add(new Span { Text = " highlight is here" });
			return formatted;
		}

		static FormattedString CreateUpdatedText()
		{
			var formatted = new FormattedString();
			formatted.Spans.Add(new Span { Text = "Updated text highlights " });
			formatted.Spans.Add(new Span
			{
				Text = "END",
				BackgroundColor = Colors.Cyan
			});
			return formatted;
		}
	}
}
#endif

