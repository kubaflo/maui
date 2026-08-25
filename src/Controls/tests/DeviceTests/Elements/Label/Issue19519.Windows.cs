#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	public class Issue19519 : ControlsHandlerTestBase
	{
		const string SentinelText = "Issue19519 sentinel";
		const string UnderlinedText = "texto subrayado";
		const string StruckText = "texto está tachado";
		const string ReportedHtml =
			"<p>Este es el primer párrafo con <span style=\"text-decoration: underline;\">texto subrayado</span> para demostrar la funcionalidad.</p>" +
			"<p>Y este es el segundo párrafo, donde parte del <span style=\"text-decoration: line-through;\">texto está tachado</span> para ilustrar otro estilo.</p>";

		[Fact]
		public async Task HtmlStyleTextDecorationsAreRendered()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var referenceText = new FormattedString();
			referenceText.Spans.Add(new Span { Text = "Este es el primer párrafo con " });
			referenceText.Spans.Add(new Span { Text = UnderlinedText, TextDecorations = TextDecorations.Underline });
			referenceText.Spans.Add(new Span { Text = " para demostrar la funcionalidad.\n\nY este es el segundo párrafo, donde parte del " });
			referenceText.Spans.Add(new Span { Text = StruckText, TextDecorations = TextDecorations.Strikethrough });
			referenceText.Spans.Add(new Span { Text = " para ilustrar otro estilo." });

			var referenceLabel = new Label { FormattedText = referenceText };
			var renderButton = new Button { Text = "Render reported HTML" };
			var affectedLabel = new Label { Text = SentinelText, TextType = TextType.Html };

			renderButton.Clicked += (_, _) =>
			{
				affectedLabel.Text = ReportedHtml;
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = new Thickness(24),
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "Expected reference: the first marked phrase is underlined and the second is struck through.",
							FontAttributes = FontAttributes.Bold
						},
						referenceLabel,
						renderButton,
						new Label { Text = "Affected HTML Label:", FontAttributes = FontAttributes.Bold },
						affectedLabel
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(referenceLabel.Handler);
				Assert.NotNull(affectedLabel.Handler);
				Assert.NotNull(renderButton.Handler);

				var referenceTextBlock = Assert.IsType<WTextBlock>(referenceLabel.Handler.PlatformView);
				var affectedTextBlock = Assert.IsType<WTextBlock>(affectedLabel.Handler.PlatformView);
				var platformButton = Assert.IsAssignableFrom<WButton>(renderButton.Handler.PlatformView);

				var referenceUnderlineRun = FindRun(referenceTextBlock.Inlines, UnderlinedText);
				var referenceStruckRun = FindRun(referenceTextBlock.Inlines, StruckText);
				Assert.NotNull(referenceUnderlineRun);
				Assert.NotNull(referenceStruckRun);
				Assert.True(HasDecoration(referenceUnderlineRun, WTextDecorations.Underline));
				Assert.False(HasDecoration(referenceUnderlineRun, WTextDecorations.Strikethrough));
				Assert.True(HasDecoration(referenceStruckRun, WTextDecorations.Strikethrough));
				Assert.False(HasDecoration(referenceStruckRun, WTextDecorations.Underline));
				Assert.True(ContainsText(affectedTextBlock.Inlines, SentinelText));

				var automationPeer = new ButtonAutomationPeer(platformButton);
				var invokeProvider = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				Assert.NotNull(invokeProvider);
				invokeProvider.Invoke();

				await AssertEventually(
					() => ContainsText(affectedTextBlock.Inlines, UnderlinedText),
					message: $"The affected Label never rendered '{UnderlinedText}'.");
				await AssertEventually(
					() => ContainsText(affectedTextBlock.Inlines, StruckText),
					message: $"The affected Label never rendered '{StruckText}'.");
				await AssertEventually(
					() => !ContainsText(affectedTextBlock.Inlines, SentinelText),
					message: "The affected Label did not replace its sentinel text.");

				var affectedUnderlineRun = FindRun(affectedTextBlock.Inlines, UnderlinedText);
				var affectedStruckRun = FindRun(affectedTextBlock.Inlines, StruckText);
				Assert.NotNull(affectedUnderlineRun);
				Assert.NotNull(affectedStruckRun);

				var hasUnderline = HasDecoration(affectedUnderlineRun, WTextDecorations.Underline);
				var hasStrikethrough = HasDecoration(affectedStruckRun, WTextDecorations.Strikethrough);
				Assert.True(
					hasUnderline && hasStrikethrough,
					$"Issue19519 HTML decoration mismatch: underline={hasUnderline} expected true; strikethrough={hasStrikethrough} expected true.");
			});
		}

		static WRun FindRun(WInlineCollection inlines, string text)
		{
			for (var i = 0; i < inlines.Count; i++)
			{
				var inline = inlines[i];
				if (inline is WRun run && run.Text == text)
					return run;

				if (inline is WSpan span)
				{
					var nestedRun = FindRun(span.Inlines, text);
					if (nestedRun is not null)
						return nestedRun;
				}
			}

			return null;
		}

		static bool ContainsText(WInlineCollection inlines, string text)
		{
			for (var i = 0; i < inlines.Count; i++)
			{
				var inline = inlines[i];
				if (inline is WRun run && run.Text.Contains(text, System.StringComparison.Ordinal))
					return true;

				if (inline is WSpan span && ContainsText(span.Inlines, text))
					return true;
			}

			return false;
		}

		static bool HasDecoration(WRun run, WTextDecorations decoration) =>
			(run.TextDecorations & decoration) == decoration;
	}
}
#endif

