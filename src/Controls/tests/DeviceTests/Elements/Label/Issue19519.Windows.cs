#if WINDOWS
using System;
using System.Text;
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
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var label = new Label
			{
				FontSize = 18,
				Text = "<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p><p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>",
				TextType = TextType.Html,
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
			};
			stack.Add(label);

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = stack,
				},
			};

			var loaded = false;
			var loadedCompletion = new TaskCompletionSource<bool>();
			label.Loaded += (_, _) =>
			{
				loaded = true;
				loadedCompletion.TrySetResult(true);
			};

			await AttachAndRun(page, async _ =>
			{
				var loadedResult = await loadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.True(loadedResult, "The HTML Label Loaded callback did not complete.");
				Assert.True(loaded, "The HTML Label Loaded callback did not change the sentinel.");

				Assert.NotNull(label.Handler);
				var textBlock = Assert.IsType<WTextBlock>(label.Handler.PlatformView);

				var underlinePhraseFound = false;
				await AssertionExtensions.AssertEventually(
					() => underlinePhraseFound = GetInlineText(textBlock.Inlines).Contains(UnderlinedPhrase, StringComparison.Ordinal),
					message: $"The Windows TextBlock did not contain \"{UnderlinedPhrase}\" after attachment.");

				var strikethroughPhraseFound = false;
				await AssertionExtensions.AssertEventually(
					() => strikethroughPhraseFound = GetInlineText(textBlock.Inlines).Contains(StruckThroughPhrase, StringComparison.Ordinal),
					message: $"The Windows TextBlock did not contain \"{StruckThroughPhrase}\" after attachment.");

				Assert.True(underlinePhraseFound, $"The Windows TextBlock did not contain \"{UnderlinedPhrase}\".");
				Assert.True(strikethroughPhraseFound, $"The Windows TextBlock did not contain \"{StruckThroughPhrase}\".");

				var underlineDecorationsFound = false;
				var underlineObserved = WTextDecorations.None;
				await AssertionExtensions.AssertEventually(
					() => underlineDecorationsFound = TryGetDecorations(
						textBlock.Inlines,
						UnderlinedPhrase,
						WTextDecorations.None,
						out underlineObserved),
					message: $"The Windows TextBlock did not contain \"{UnderlinedPhrase}\" after attachment.");

				var strikethroughDecorationsFound = false;
				var strikethroughObserved = WTextDecorations.None;
				await AssertionExtensions.AssertEventually(
					() => strikethroughDecorationsFound = TryGetDecorations(
						textBlock.Inlines,
						StruckThroughPhrase,
						WTextDecorations.None,
						out strikethroughObserved),
					message: $"The Windows TextBlock did not contain \"{StruckThroughPhrase}\" after attachment.");

				Assert.True(underlineDecorationsFound, $"The Windows TextBlock did not contain \"{UnderlinedPhrase}\" while inspecting decorations.");
				Assert.True(strikethroughDecorationsFound, $"The Windows TextBlock did not contain \"{StruckThroughPhrase}\" while inspecting decorations.");
				Assert.True(
					(underlineObserved & WTextDecorations.Underline) == WTextDecorations.Underline,
					$"Issue19519 HTML underline missing for \"{UnderlinedPhrase}\". Observed: {underlineObserved}; expected: {WTextDecorations.Underline}.");
				Assert.True(
					(strikethroughObserved & WTextDecorations.Strikethrough) == WTextDecorations.Strikethrough,
					$"Issue19519 HTML strikethrough missing for \"{StruckThroughPhrase}\". Observed: {strikethroughObserved}; expected: {WTextDecorations.Strikethrough}.");
			});
		}

		static string GetInlineText(WInlineCollection inlines)
		{
			var text = new StringBuilder();

			foreach (var inline in inlines)
			{
				if (inline is WRun run)
					text.Append(run.Text);

				if (inline is WSpan span)
					text.Append(GetInlineText(span.Inlines));
			}

			return text.ToString();
		}

		static bool TryGetDecorations(
			WInlineCollection inlines,
			string expectedText,
			WTextDecorations inheritedDecorations,
			out WTextDecorations observedDecorations)
		{
			foreach (var inline in inlines)
			{
				var decorations = inheritedDecorations;

				if (inline is WUnderline)
					decorations |= WTextDecorations.Underline;

				if (inline is WRun run)
				{
					decorations |= run.TextDecorations;

					if (run.Text.Contains(expectedText, StringComparison.Ordinal))
					{
						observedDecorations = decorations;
						return true;
					}
				}

				if (inline is WSpan span &&
					TryGetDecorations(span.Inlines, expectedText, decorations, out observedDecorations))
				{
					return true;
				}
			}

			observedDecorations = WTextDecorations.None;
			return false;
		}
	}
}
#endif

