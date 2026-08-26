#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	public class Issue19519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlLabelRendersUnderlineAndStrikethroughDecorations()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var referenceText = new FormattedString();
			referenceText.Spans.Add(new Span { Text = "Este es el primer párrafo con " });
			referenceText.Spans.Add(new Span { Text = "texto subrayado", TextDecorations = TextDecorations.Underline });
			referenceText.Spans.Add(new Span { Text = " para demostrar la funcionalidad.\n\nY este es el segundo párrafo, donde parte del " });
			referenceText.Spans.Add(new Span { Text = "texto está tachado", TextDecorations = TextDecorations.Strikethrough });
			referenceText.Spans.Add(new Span { Text = " para ilustrar otro estilo." });

			var referenceLabel = new Label
			{
				FormattedText = referenceText
			};

			var htmlLabel = new Label
			{
				TextType = TextType.Html,
				Text = """
					<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p>
					<p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>
					"""
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 18,
						Children =
						{
							referenceLabel,
							htmlLabel
						}
					}
				}
			};

			var attachmentSentinel = -1;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var referenceAttached = await Wait(
					() => referenceLabel.Handler is LabelHandler { PlatformView: WTextBlock },
					timeout: 2000);
				Assert.True(referenceAttached, "The native-span reference Label did not attach to a WinUI TextBlock.");

				var htmlAttached = await Wait(
					() => htmlLabel.Handler is LabelHandler { PlatformView: WTextBlock },
					timeout: 2000);
				Assert.True(htmlAttached, "The HTML Label did not attach to a WinUI TextBlock.");

				var referenceTextBlock = Assert.IsType<WTextBlock>(
					Assert.IsType<LabelHandler>(referenceLabel.Handler).PlatformView);
				var htmlTextBlock = Assert.IsType<WTextBlock>(
					Assert.IsType<LabelHandler>(htmlLabel.Handler).PlatformView);

				attachmentSentinel = 1;
				Assert.Equal(1, attachmentSentinel);

				var referenceUnderline = FindDecoration(
					referenceTextBlock.Inlines,
					"texto subrayado",
					WTextDecorations.Underline);
				var referenceStrikethrough = FindDecoration(
					referenceTextBlock.Inlines,
					"texto está tachado",
					WTextDecorations.Strikethrough);

				Assert.True(referenceUnderline.TextFound, "The native-span reference did not contain the underlined text.");
				Assert.True(referenceStrikethrough.TextFound, "The native-span reference did not contain the struck text.");
				Assert.True(
					referenceUnderline.Decorated && referenceStrikethrough.Decorated,
					"The native-span reference did not render both text decorations.");

				var htmlUnderline = FindDecoration(
					htmlTextBlock.Inlines,
					"texto subrayado",
					WTextDecorations.Underline);
				var htmlStrikethrough = FindDecoration(
					htmlTextBlock.Inlines,
					"texto está tachado",
					WTextDecorations.Strikethrough);

				Assert.True(htmlUnderline.TextFound, "The HTML Label did not contain the underlined text.");
				Assert.True(htmlStrikethrough.TextFound, "The HTML Label did not contain the struck text.");
				Assert.True(
					htmlUnderline.Decorated && htmlStrikethrough.Decorated,
					$"Windows HTML Label native inline decorations were incorrect: underline observed={htmlUnderline.Decorated}, expected=True; strikethrough observed={htmlStrikethrough.Decorated}, expected=True.");
			});

			Assert.Equal(1, attachmentSentinel);
		}

		static (bool TextFound, bool Decorated) FindDecoration(
			WInlineCollection inlines,
			string expectedText,
			WTextDecorations expectedDecoration,
			WTextDecorations inheritedDecorations = WTextDecorations.None)
		{
			foreach (var inline in inlines)
			{
				var effectiveDecorations = inheritedDecorations | inline.TextDecorations;

				if (inline is WRun run &&
					run.Text.Contains(expectedText, StringComparison.Ordinal))
				{
					return (true, (effectiveDecorations & expectedDecoration) != 0);
				}

				if (inline is WSpan span)
				{
					var result = FindDecoration(
						span.Inlines,
						expectedText,
						expectedDecoration,
						effectiveDecorations);

					if (result.TextFound)
						return result;
				}
			}

			return (false, false);
		}
	}
}
#endif

