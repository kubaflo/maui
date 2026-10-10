#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WRun = Microsoft.UI.Xaml.Documents.Run;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;
using WUnderline = Microsoft.UI.Xaml.Documents.Underline;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	public class Issue19519 : ControlsHandlerTestBase
	{
		const string UnderlinedPhrase = "texto subrayado";
		const string StruckThroughPhrase = "texto está tachado";

		[Fact]
		public async Task HtmlUnderlineAndStrikethroughAreApplied()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var affectedLabel = new Label
			{
				Text = "<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p><p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>",
				TextType = TextType.Html,
			};

			var referenceLabel = new Label
			{
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = UnderlinedPhrase, TextDecorations = TextDecorations.Underline },
						new Span { Text = " | " },
						new Span { Text = StruckThroughPhrase, TextDecorations = TextDecorations.Strikethrough },
					},
				},
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "Windows HTML Label styling reproduction" },
					affectedLabel,
					new Label { Text = "Expected styled phrases:" },
					referenceLabel,
				},
			};
			var page = new ContentPage { Content = layout };

			LabelHandler affectedHandler = null;
			WTextBlock affectedTextBlock = null;
			var affectedTextBlockLoaded = false;
			affectedLabel.HandlerChanged += (_, _) =>
			{
				if (affectedLabel.Handler is LabelHandler labelHandler)
				{
					affectedHandler = labelHandler;
					affectedTextBlock = labelHandler.PlatformView;
					affectedTextBlock.Loaded += (_, _) => affectedTextBlockLoaded = true;
				}
			};

			await AttachAndRun(page, async _ =>
			{
				await AssertEventually(
					() => affectedTextBlockLoaded,
					timeout: 5000,
					message: "The affected Windows TextBlock did not load.");
				Assert.True(affectedTextBlockLoaded, "The affected Windows TextBlock Loaded event should occur after attachment.");

				await AssertEventually(
					() => affectedTextBlock is not null &&
						ContainsPhrase(affectedTextBlock.Inlines, UnderlinedPhrase) &&
						ContainsPhrase(affectedTextBlock.Inlines, StruckThroughPhrase),
					timeout: 5000,
					message: "The affected Windows TextBlock did not populate both HTML phrases.");

				Assert.NotNull(affectedLabel.Handler);
				Assert.NotNull(affectedLabel.Handler.PlatformView);
				Assert.Same(affectedHandler, affectedLabel.Handler);
				Assert.NotNull(affectedTextBlock);

				Assert.NotNull(referenceLabel.Handler);
				Assert.NotNull(referenceLabel.Handler.PlatformView);
				var referenceHandler = Assert.IsType<LabelHandler>(referenceLabel.Handler);
				var referenceTextBlock = Assert.IsType<WTextBlock>(referenceHandler.PlatformView);

				var referenceUnderline = GetPhraseDecorations(referenceTextBlock.Inlines, UnderlinedPhrase);
				Assert.True(referenceUnderline.Found, $"Reference phrase '{UnderlinedPhrase}' should exist.");
				Assert.True(referenceUnderline.Underline, $"Reference phrase '{UnderlinedPhrase}' should be underlined.");

				var referenceStrikethrough = GetPhraseDecorations(referenceTextBlock.Inlines, StruckThroughPhrase);
				Assert.True(referenceStrikethrough.Found, $"Reference phrase '{StruckThroughPhrase}' should exist.");
				Assert.True(referenceStrikethrough.Strikethrough, $"Reference phrase '{StruckThroughPhrase}' should be struck through.");

				var affectedUnderline = GetPhraseDecorations(affectedTextBlock.Inlines, UnderlinedPhrase);
				Assert.True(affectedUnderline.Found, $"HTML phrase '{UnderlinedPhrase}' should exist.");
				Assert.True(
					affectedUnderline.Underline,
					$"Issue19519 HTML underline missing: expected=True, observed={affectedUnderline.Underline}");

				var affectedStrikethrough = GetPhraseDecorations(affectedTextBlock.Inlines, StruckThroughPhrase);
				Assert.True(affectedStrikethrough.Found, $"HTML phrase '{StruckThroughPhrase}' should exist.");
				Assert.True(
					affectedStrikethrough.Strikethrough,
					$"Issue19519 HTML strikethrough missing: expected=True, observed={affectedStrikethrough.Strikethrough}");
			});
		}

		static bool ContainsPhrase(WInlineCollection inlines, string phrase) =>
			GetPhraseDecorations(inlines, phrase).Found;

		static (bool Found, bool Underline, bool Strikethrough) GetPhraseDecorations(
			WInlineCollection inlines,
			string phrase,
			bool inheritedUnderline = false,
			bool inheritedStrikethrough = false)
		{
			foreach (var inline in inlines)
			{
				var decorations = inline.TextDecorations;
				var hasUnderline = inheritedUnderline ||
					inline is WUnderline ||
					(decorations & WTextDecorations.Underline) != 0;
				var hasStrikethrough = inheritedStrikethrough ||
					(decorations & WTextDecorations.Strikethrough) != 0;

				if (inline is WRun run &&
					run.Text?.Contains(phrase, StringComparison.Ordinal) == true)
				{
					return (true, hasUnderline, hasStrikethrough);
				}

				if (inline is WSpan span)
				{
					var result = GetPhraseDecorations(
						span.Inlines,
						phrase,
						hasUnderline,
						hasStrikethrough);

					if (result.Found)
						return result;
				}
			}

			return (false, false, false);
		}
	}
}
#endif

