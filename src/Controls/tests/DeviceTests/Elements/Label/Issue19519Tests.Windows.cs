using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;
using WUnderline = Microsoft.UI.Xaml.Documents.Underline;

namespace Microsoft.Maui.DeviceTests
{
	public class Issue19519 : ControlsHandlerTestBase
	{
		const string UnderlinedText = "texto subrayado";
		const string StruckText = "texto está tachado";

		[Fact]
		[Category("Issue19519")]
		public async Task HtmlLabelPreservesUnderlineAndStrikethrough()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var referenceLabel = new Label
			{
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = UnderlinedText, TextDecorations = TextDecorations.Underline },
						new Span { Text = StruckText, TextDecorations = TextDecorations.Strikethrough }
					}
				}
			};

			await AttachAndRun(referenceLabel, (LabelHandler handler) =>
			{
				var textBlock = Assert.IsType<WTextBlock>(handler.PlatformView);
				var decorations = InspectDecorations(textBlock.Inlines);

				Assert.True(decorations.Underline.Visited, $"Reference run '{UnderlinedText}' was not found.");
				Assert.True(
					(decorations.Underline.Decorations & WTextDecorations.Underline) != 0,
					$"Reference underline was missing from '{UnderlinedText}': {decorations.Underline.Decorations}");
				Assert.True(decorations.Strikethrough.Visited, $"Reference run '{StruckText}' was not found.");
				Assert.True(
					(decorations.Strikethrough.Decorations & WTextDecorations.Strikethrough) != 0,
					$"Reference strikethrough was missing from '{StruckText}': {decorations.Strikethrough.Decorations}");
			});

			var htmlLabel = new Label
			{
				Text = "<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p><p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>",
				TextType = TextType.Html
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 20,
					Children =
					{
						new Label { Text = "The label below uses TextType=Html and default styling." },
						htmlLabel,
						new Button { Text = "Check HTML rendering" },
						new Label { Text = "HTML rendering result", FontAttributes = FontAttributes.Bold }
					}
				}
			};

			await AttachAndRun(page, (PageHandler _) =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(htmlLabel.Handler);
				var textBlock = Assert.IsType<WTextBlock>(labelHandler.PlatformView);
				var decorations = InspectDecorations(textBlock.Inlines);

				Assert.True(decorations.Underline.Visited, $"HTML run '{UnderlinedText}' was not found.");
				Assert.True(decorations.Strikethrough.Visited, $"HTML run '{StruckText}' was not found.");
				Assert.True(
					(decorations.Underline.Decorations & WTextDecorations.Underline) != 0,
					$"HTML underline was missing from '{UnderlinedText}': {decorations.Underline.Decorations}");
				Assert.True(
					(decorations.Strikethrough.Decorations & WTextDecorations.Strikethrough) != 0,
					$"HTML strikethrough was missing from '{StruckText}': {decorations.Strikethrough.Decorations}");
			});
		}

		static DecorationInspection InspectDecorations(WInlineCollection inlines)
		{
			var underline = new RunDecoration(false, WTextDecorations.None);
			var strikethrough = new RunDecoration(false, WTextDecorations.None);
			InspectDecorations(inlines, WTextDecorations.None, ref underline, ref strikethrough);
			return new DecorationInspection(underline, strikethrough);
		}

		static void InspectDecorations(
			WInlineCollection inlines,
			WTextDecorations inheritedDecorations,
			ref RunDecoration underline,
			ref RunDecoration strikethrough)
		{
			foreach (var inline in inlines)
			{
				var decorations = inheritedDecorations | inline.TextDecorations;
				if (inline is WUnderline)
					decorations |= WTextDecorations.Underline;

				if (inline is WRun run)
				{
					if (run.Text.Contains(UnderlinedText, StringComparison.Ordinal))
						underline = new RunDecoration(true, decorations);

					if (run.Text.Contains(StruckText, StringComparison.Ordinal))
						strikethrough = new RunDecoration(true, decorations);
				}
				else if (inline is WSpan span)
				{
					InspectDecorations(span.Inlines, decorations, ref underline, ref strikethrough);
				}
			}
		}

		readonly struct DecorationInspection
		{
			public DecorationInspection(RunDecoration underline, RunDecoration strikethrough)
			{
				Underline = underline;
				Strikethrough = strikethrough;
			}

			public RunDecoration Underline { get; }

			public RunDecoration Strikethrough { get; }
		}

		readonly struct RunDecoration
		{
			public RunDecoration(bool visited, WTextDecorations decorations)
			{
				Visited = visited;
				Decorations = decorations;
			}

			public bool Visited { get; }

			public WTextDecorations Decorations { get; }
		}
	}
}

