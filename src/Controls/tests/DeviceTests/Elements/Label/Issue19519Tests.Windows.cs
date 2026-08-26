#if WINDOWS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WInline = Microsoft.UI.Xaml.Documents.Inline;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WLineBreak = Microsoft.UI.Xaml.Documents.LineBreak;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;
using WUnderline = Microsoft.UI.Xaml.Documents.Underline;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue19519 : ControlsHandlerTestBase
	{
		const string UnderlinedPhrase = "texto subrayado";
		const string StruckThroughPhrase = "texto está tachado";
		const string HtmlText =
			"<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p>" +
			"<p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>";

		[Fact]
		public async Task HtmlUnderlineAndStrikethroughAreApplied()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
				});
			});

			var underlinedSpan = new Microsoft.Maui.Controls.Span
			{
				Text = UnderlinedPhrase,
				TextDecorations = TextDecorations.Underline
			};
			var struckThroughSpan = new Microsoft.Maui.Controls.Span
			{
				Text = StruckThroughPhrase,
				TextDecorations = TextDecorations.Strikethrough
			};
			var formattedText = new FormattedString();
			formattedText.Spans.Add(underlinedSpan);
			formattedText.Spans.Add(new Microsoft.Maui.Controls.Span { Text = " / " });
			formattedText.Spans.Add(struckThroughSpan);

			var htmlLabel = new Label
			{
				Text = HtmlText,
				TextType = TextType.Html
			};
			var referenceLabel = new Label
			{
				FormattedText = formattedText
			};
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "HTML label (affected control)"
					},
					htmlLabel,
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Expected decorated text reference"
					},
					referenceLabel
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = content }
			};

			Assert.Equal(HtmlText, htmlLabel.Text);
			Assert.Equal(TextType.Html, htmlLabel.TextType);
			Assert.Same(formattedText, referenceLabel.FormattedText);
			Assert.Equal(TextDecorations.Underline, underlinedSpan.TextDecorations);
			Assert.Equal(TextDecorations.Strikethrough, struckThroughSpan.TextDecorations);

			var attachmentSentinel = -1;
			(string Value, IReadOnlyList<WTextDecorations> Decorations) htmlNativeText = default;
			(string Value, IReadOnlyList<WTextDecorations> Decorations) referenceNativeText = default;

			await CreateHandlerAndAddToWindow(page, () =>
			{
				Assert.NotNull(htmlLabel.Handler);
				Assert.NotNull(referenceLabel.Handler);

				var htmlTextBlock = Assert.IsType<WTextBlock>(htmlLabel.Handler.PlatformView);
				var referenceTextBlock = Assert.IsType<WTextBlock>(referenceLabel.Handler.PlatformView);

				Assert.True(htmlTextBlock.IsLoaded);
				Assert.True(referenceTextBlock.IsLoaded);
				Assert.True(htmlTextBlock.ActualWidth > 0 && htmlTextBlock.ActualHeight > 0);
				Assert.True(referenceTextBlock.ActualWidth > 0 && referenceTextBlock.ActualHeight > 0);

				htmlNativeText = ReadNativeText(htmlTextBlock);
				referenceNativeText = ReadNativeText(referenceTextBlock);
				attachmentSentinel = 1;
			});

			Assert.Equal(1, attachmentSentinel);
			Assert.NotNull(htmlNativeText.Value);
			Assert.NotNull(htmlNativeText.Decorations);
			Assert.NotNull(referenceNativeText.Value);
			Assert.NotNull(referenceNativeText.Decorations);

			AssertSegmentDecoration(
				referenceNativeText,
				UnderlinedPhrase,
				(WTextDecorations)underlinedSpan.TextDecorations,
				"formatted reference underline",
				false);
			AssertSegmentDecoration(
				referenceNativeText,
				StruckThroughPhrase,
				(WTextDecorations)struckThroughSpan.TextDecorations,
				"formatted reference strikethrough",
				false);

			AssertSegmentDecoration(
				htmlNativeText,
				UnderlinedPhrase,
				WTextDecorations.Underline,
				"HTML u element",
				true);
			AssertSegmentDecoration(
				htmlNativeText,
				StruckThroughPhrase,
				WTextDecorations.Strikethrough,
				"HTML s element",
				true);
		}

		static (string Value, IReadOnlyList<WTextDecorations> Decorations) ReadNativeText(WTextBlock textBlock)
		{
			var text = new StringBuilder();
			var decorations = new List<WTextDecorations>();

			if (textBlock.Inlines.Count == 0)
			{
				AppendText(textBlock.Text, textBlock.TextDecorations, text, decorations);
			}
			else
			{
				AppendInlines(textBlock.Inlines, textBlock.TextDecorations, text, decorations);
			}

			return (text.ToString(), decorations);
		}

		static void AppendInlines(
			WInlineCollection inlines,
			WTextDecorations inheritedDecorations,
			StringBuilder text,
			List<WTextDecorations> decorations)
		{
			foreach (WInline inline in inlines)
			{
				var effectiveDecorations = inheritedDecorations | inline.TextDecorations;
				if (inline is WUnderline)
					effectiveDecorations |= WTextDecorations.Underline;

				if (inline is WRun run)
				{
					AppendText(run.Text, effectiveDecorations, text, decorations);
				}
				else if (inline is WSpan span)
				{
					AppendInlines(span.Inlines, effectiveDecorations, text, decorations);
				}
				else if (inline is WLineBreak)
				{
					AppendText("\r\n", effectiveDecorations, text, decorations);
				}
			}
		}

		static void AppendText(
			string value,
			WTextDecorations effectiveDecorations,
			StringBuilder text,
			List<WTextDecorations> decorations)
		{
			text.Append(value);
			for (var i = 0; i < value.Length; i++)
				decorations.Add(effectiveDecorations);
		}

		static void AssertSegmentDecoration(
			(string Value, IReadOnlyList<WTextDecorations> Decorations) nativeText,
			string phrase,
			WTextDecorations expected,
			string condition,
			bool isHtml)
		{
			var index = nativeText.Value.IndexOf(phrase, StringComparison.Ordinal);
			var occursExactlyOnce =
				index >= 0 &&
				nativeText.Value.LastIndexOf(phrase, StringComparison.Ordinal) == index;
			var locationMessage = isHtml
				? $"HTML text decoration mismatch: {condition}; phrase '{phrase}' must occur exactly once in native text."
				: $"{condition}: phrase '{phrase}' must occur exactly once in native text.";
			Assert.True(occursExactlyOnce, locationMessage);

			var observed = WTextDecorations.None;
			var hasExpectedDecoration = true;
			for (var i = index; i < index + phrase.Length; i++)
			{
				observed |= nativeText.Decorations[i];
				hasExpectedDecoration &= nativeText.Decorations[i] == expected;
			}
			var decorationMessage = isHtml
				? $"HTML text decoration mismatch: {condition}; phrase '{phrase}', observed {observed}, expected {expected}."
				: $"{condition}: phrase '{phrase}', observed {observed}, expected {expected}.";
			Assert.True(hasExpectedDecoration, decorationMessage);
		}

	}
}
#endif

