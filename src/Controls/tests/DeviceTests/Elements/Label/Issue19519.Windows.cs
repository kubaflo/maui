using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Documents;
using Xunit;
using NativeSpan = Microsoft.UI.Xaml.Documents.Span;
using NativeTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using NativeTextDecorations = Windows.UI.Text.TextDecorations;

namespace Microsoft.Maui.DeviceTests
{
	[Category(nameof(Issue19519))]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue19519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlInsAndDelRenderWithTextDecorations()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var htmlLabel = new Label
			{
				TextType = TextType.Html,
				Text = "<p>Este es el primer párrafo con <ins>texto subrayado</ins> para demostrar la funcionalidad.</p>" +
					"<p>Y este es el segundo párrafo, donde parte del <del>texto está tachado</del> para ilustrar otro estilo.</p>"
			};

			var expectedDecorations = new FormattedString();
			expectedDecorations.Spans.Add(new Microsoft.Maui.Controls.Span
			{
				Text = "texto subrayado",
				TextDecorations = TextDecorations.Underline
			});
			expectedDecorations.Spans.Add(new Microsoft.Maui.Controls.Span { Text = " / " });
			expectedDecorations.Spans.Add(new Microsoft.Maui.Controls.Span
			{
				Text = "texto está tachado",
				TextDecorations = TextDecorations.Strikethrough
			});

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					new Label { Text = "HTML rendering:" },
					htmlLabel,
					new Label { Text = "Expected decorations:" },
					new Label { FormattedText = expectedDecorations },
					new Label { Text = "Checking HTML decorations" }
				}
			};

			var pageLoaded = false;
			var loadedCompletion = new TaskCompletionSource<bool>();
			page.Loaded += (_, _) =>
			{
				pageLoaded = true;
				loadedCompletion.TrySetResult(true);
			};

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				await loadedCompletion.Task;
				Assert.True(pageLoaded, "The ContentPage Loaded callback should occur after attachment.");

				var textBlock = Assert.IsType<NativeTextBlock>(htmlLabel.Handler.PlatformView);

				Assert.True(
					TryGetRunDecorations(textBlock.Inlines, "texto subrayado", NativeTextDecorations.None, out var underlineDecorations),
					"The native Windows TextBlock should contain the HTML <ins> text.");
				Assert.True(
					(underlineDecorations & NativeTextDecorations.Underline) != 0,
					"HTML <ins> text should render as an underline in the native Windows TextBlock.");

				Assert.True(
					TryGetRunDecorations(textBlock.Inlines, "texto está tachado", NativeTextDecorations.None, out var strikethroughDecorations),
					"The native Windows TextBlock should contain the HTML <del> text.");
				Assert.True(
					(strikethroughDecorations & NativeTextDecorations.Strikethrough) != 0,
					"HTML <del> text should render with strikethrough in the native Windows TextBlock.");
			});
		}

		static bool TryGetRunDecorations(
			InlineCollection inlines,
			string text,
			NativeTextDecorations inheritedDecorations,
			out NativeTextDecorations decorations)
		{
			foreach (var inline in inlines)
			{
				if (inline is Run run && run.Text == text)
				{
					decorations = inheritedDecorations | run.TextDecorations;
					return true;
				}

				if (inline is NativeSpan span)
				{
					var spanDecorations = inheritedDecorations | span.TextDecorations;
					if (span is Underline)
						spanDecorations |= NativeTextDecorations.Underline;

					if (TryGetRunDecorations(span.Inlines, text, spanDecorations, out decorations))
						return true;
				}
			}

			decorations = NativeTextDecorations.None;
			return false;
		}
	}
}
