#if WINDOWS
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WLineBreak = Microsoft.UI.Xaml.Documents.LineBreak;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;
using WUnderline = Microsoft.UI.Xaml.Documents.Underline;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	public class Issue19519 : ControlsHandlerTestBase
	{
		const string UnderlinedText = "texto subrayado";
		const string StruckText = "texto está tachado";

		[Fact]
		public async Task HtmlTextDecorationsAreApplied()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var (page, htmlLabel, referenceLabel) = await InvokeOnMainThreadAsync(() =>
			{
				var htmlLabel = new Label
				{
					FontSize = 20,
					TextType = TextType.Html,
					Text = $"<p>Este es el primer párrafo con <u>{UnderlinedText}</u> para demostrar la funcionalidad.</p>" +
						$"<p>Y este es el segundo párrafo, donde parte del <s>{StruckText}</s> para ilustrar otro estilo.</p>",
				};

				var referenceLabel = new Label
				{
					FontSize = 20,
					FormattedText = new FormattedString
					{
						Spans =
						{
							new Span { Text = UnderlinedText, TextDecorations = TextDecorations.Underline },
							new Span { Text = " / " },
							new Span { Text = StruckText, TextDecorations = TextDecorations.Strikethrough },
						},
					},
				};

				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						htmlLabel,
						referenceLabel,
					},
				};

				var page = new ContentPage
				{
					Content = new ScrollView { Content = layout },
				};

				return (page, htmlLabel, referenceLabel);
			});

			var attachmentObserved = false;
			WTextBlock htmlPlatformView = null;
			WTextBlock referencePlatformView = null;

			await AttachAndRun(page, async _ =>
			{
				attachmentObserved = true;

				var htmlHandler = Assert.IsType<LabelHandler>(htmlLabel.Handler);
				var referenceHandler = Assert.IsType<LabelHandler>(referenceLabel.Handler);
				htmlPlatformView = htmlHandler.PlatformView;
				referencePlatformView = referenceHandler.PlatformView;

				Assert.NotNull(htmlPlatformView);
				Assert.NotNull(referencePlatformView);

				var referenceUnderline = unchecked((WTextDecorations)(-1));
				var referenceStrikethrough = unchecked((WTextDecorations)(-1));
				var htmlUnderline = unchecked((WTextDecorations)(-1));
				var htmlStrikethrough = unchecked((WTextDecorations)(-1));

				await AssertEventually(
					() =>
						TryGetDecorations(referencePlatformView.Inlines, UnderlinedText, (WTextDecorations)0, out referenceUnderline) &&
						TryGetDecorations(referencePlatformView.Inlines, StruckText, (WTextDecorations)0, out referenceStrikethrough) &&
						TryGetDecorations(htmlPlatformView.Inlines, UnderlinedText, (WTextDecorations)0, out htmlUnderline) &&
						TryGetDecorations(htmlPlatformView.Inlines, StruckText, (WTextDecorations)0, out htmlStrikethrough),
					message: "The attached labels did not expose the expected native inline text.");

				var referenceText = FlattenText(referencePlatformView.Inlines);
				Assert.Contains(UnderlinedText, referenceText, System.StringComparison.Ordinal);
				Assert.Contains(StruckText, referenceText, System.StringComparison.Ordinal);
				Assert.True((referenceUnderline & WTextDecorations.Underline) != 0, "The FormattedString reference did not render its underline.");
				Assert.True((referenceStrikethrough & WTextDecorations.Strikethrough) != 0, "The FormattedString reference did not render its strikethrough.");

				var htmlText = FlattenText(htmlPlatformView.Inlines);
				var htmlInlineCount = htmlPlatformView.Inlines.Count;
				Assert.Contains(UnderlinedText, htmlText, System.StringComparison.Ordinal);
				Assert.Contains(StruckText, htmlText, System.StringComparison.Ordinal);
				Assert.True(htmlInlineCount > 0, "The HTML label did not render any native inlines.");
				Assert.True(
					(htmlUnderline & WTextDecorations.Underline) != 0,
					$"HTML <u> text '{UnderlinedText}' was rendered without underline decoration. Observed={htmlUnderline}; Text='{htmlText}'; InlineCount={htmlInlineCount}.");
				Assert.True(
					(htmlStrikethrough & WTextDecorations.Strikethrough) != 0,
					$"HTML <s> text '{StruckText}' was rendered without strikethrough decoration. Observed={htmlStrikethrough}; Text='{htmlText}'; InlineCount={htmlInlineCount}.");
			});

			Assert.True(attachmentObserved, "The post-attachment callback did not run.");
			Assert.NotNull(htmlPlatformView);
			Assert.NotNull(referencePlatformView);
		}

		static bool TryGetDecorations(
			WInlineCollection inlines,
			string expectedText,
			WTextDecorations inheritedDecorations,
			out WTextDecorations decorations)
		{
			foreach (var inline in inlines)
			{
				if (inline is WRun run)
				{
					if (run.Text == expectedText)
					{
						decorations = inheritedDecorations | run.TextDecorations;
						return true;
					}
				}
				else if (inline is WUnderline underline &&
					TryGetDecorations(underline.Inlines, expectedText, inheritedDecorations | WTextDecorations.Underline, out decorations))
				{
					return true;
				}
				else if (inline is WSpan span &&
					TryGetDecorations(span.Inlines, expectedText, inheritedDecorations, out decorations))
				{
					return true;
				}
			}

			decorations = (WTextDecorations)0;
			return false;
		}

		static string FlattenText(WInlineCollection inlines)
		{
			var text = new StringBuilder();
			AppendText(inlines, text);
			return text.ToString();
		}

		static void AppendText(WInlineCollection inlines, StringBuilder text)
		{
			foreach (var inline in inlines)
			{
				if (inline is WRun run)
					text.Append(run.Text);
				else if (inline is WLineBreak)
					text.AppendLine();
				else if (inline is WSpan span)
					AppendText(span.Inlines, text);
			}
		}
	}
}
#endif

