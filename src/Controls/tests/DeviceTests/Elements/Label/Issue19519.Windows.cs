using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
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
		[Fact]
		public async Task HtmlUnderlineAndStrikethroughAreRendered()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var scene = await InvokeOnMainThreadAsync(() =>
			{
				var headingLabel = new Label
				{
					Text = "Windows HTML label styling",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				};

				var htmlLabel = new Label
				{
					TextType = TextType.Html
				};

				htmlLabel.Text =
					"<p>Este es el primer párrafo con <u>texto subrayado</u> para demostrar la funcionalidad.</p>" +
					"<p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>";

				var checkButton = new Button
				{
					Text = "Check HTML styles"
				};

				var footerLabel = new Label
				{
					Text = "Rendered HTML output",
					FontSize = 18,
					FontAttributes = FontAttributes.Bold
				};

				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 20,
					Children =
					{
						headingLabel,
						htmlLabel,
						checkButton,
						footerLabel
					}
				};

				var page = new ContentPage
				{
					Title = "Issue 19519",
					Content = layout
				};

				var layoutCompleted = false;
				var layoutCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				Func<bool> isLayoutCompleted = () => layoutCompleted;
				htmlLabel.SizeChanged += (_, _) =>
				{
					layoutCompleted = true;
					layoutCompletion.TrySetResult(true);
				};

				return (Page: page, HtmlLabel: htmlLabel, LayoutCompletion: layoutCompletion, IsLayoutCompleted: isLayoutCompleted);
			});

			await CreateHandlerAndAddToWindow(scene.Page, async () =>
			{
				await scene.LayoutCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.True(scene.IsLayoutCompleted(), "The HTML label did not complete layout after window attachment.");

				var labelHandler = Assert.IsType<LabelHandler>(scene.HtmlLabel.Handler);
				var textBlock = Assert.IsType<WTextBlock>(labelHandler.PlatformView);
				var nativeText = GetNativeText(textBlock.Inlines);

				Assert.Contains("texto subrayado", nativeText, StringComparison.Ordinal);
				Assert.Contains("texto está tachado", nativeText, StringComparison.Ordinal);

				var hasUnderline = ContainsUnderline(textBlock.Inlines);
				var hasStrikethrough = (textBlock.TextDecorations & WTextDecorations.Strikethrough) != 0;

				Assert.True(
					hasUnderline && hasStrikethrough,
					$"Issue 19519 Windows HTML decorations mismatch: underline={hasUnderline}; strikethrough={hasStrikethrough}");
			});
		}

		static bool ContainsUnderline(WInlineCollection inlines)
		{
			foreach (var inline in inlines)
			{
				if (inline is WUnderline)
					return true;

				if (inline is WSpan span && ContainsUnderline(span.Inlines))
					return true;
			}

			return false;
		}

		static string GetNativeText(WInlineCollection inlines)
		{
			var text = new StringBuilder();

			foreach (var inline in inlines)
			{
				if (inline is WRun run)
					text.Append(run.Text);
				else if (inline is WSpan span)
					text.Append(GetNativeText(span.Inlines));
			}

			return text.ToString();
		}
	}
}

