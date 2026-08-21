#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WInline = Microsoft.UI.Xaml.Documents.Inline;
using WInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;
using WSpan = Microsoft.UI.Xaml.Documents.Span;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTextDecorations = Windows.UI.Text.TextDecorations;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue19519")]
	public class Issue19519 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlUnderlineAndStrikethroughRenderAsNativeDecorations()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			const string html =
				"<p>Este es el primer párrafo con <u> texto subrayado </u> para demostrar la funcionalidad.</p>" +
				"<p>Y este es el segundo párrafo, donde parte del <s>texto está tachado</s> para ilustrar otro estilo.</p>";

			var referenceLabel = new Label
			{
				FormattedText = new FormattedString
				{
					Spans =
					{
						new Span { Text = "Reference: Este es el primer párrafo con " },
						new Span { Text = "texto subrayado", TextDecorations = TextDecorations.Underline },
						new Span { Text = "; y el segundo contiene " },
						new Span { Text = "texto tachado", TextDecorations = TextDecorations.Strikethrough },
						new Span { Text = "." },
					}
				}
			};
			var htmlLabel = new Label
			{
				Text = html,
				TextType = TextType.Html
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					referenceLabel,
					htmlLabel
				}
			};
			var scrollView = new ScrollView { Content = layout };
			var page = new ContentPage { Content = scrollView };

			var nativeLoaded = false;
			var nativeLoadedCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			htmlLabel.HandlerChanged += OnHandlerChanged;

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await nativeLoadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(nativeLoaded, "The HTML Label's native TextBlock Loaded event did not occur.");
				Assert.Same(scrollView, page.Content);
				Assert.Same(layout, scrollView.Content);
				Assert.Same(referenceLabel, layout.Children[0]);
				Assert.Same(htmlLabel, layout.Children[1]);
				Assert.Equal(html, htmlLabel.Text);
				Assert.Equal(TextType.Html, htmlLabel.TextType);

				var referenceTextBlock = Assert.IsType<WTextBlock>(referenceLabel.Handler.PlatformView);
				var htmlTextBlock = Assert.IsType<WTextBlock>(htmlLabel.Handler.PlatformView);
				Assert.NotEmpty(referenceTextBlock.Inlines);
				Assert.NotEmpty(htmlTextBlock.Inlines);

				var referenceHasUnderline = HasDecoration(referenceTextBlock.Inlines, WTextDecorations.Underline);
				var referenceHasStrikethrough = HasDecoration(referenceTextBlock.Inlines, WTextDecorations.Strikethrough);
				Assert.True(
					referenceHasUnderline && referenceHasStrikethrough,
					$"Reference Label native decorations: expected underline=True, strikethrough=True; observed underline={referenceHasUnderline}, strikethrough={referenceHasStrikethrough}");

				var htmlHasUnderline = HasDecoration(htmlTextBlock.Inlines, WTextDecorations.Underline);
				var htmlHasStrikethrough = HasDecoration(htmlTextBlock.Inlines, WTextDecorations.Strikethrough);
				Assert.True(
					htmlHasUnderline && htmlHasStrikethrough,
					$"HTML Label native decorations: expected underline=True, strikethrough=True; observed underline={htmlHasUnderline}, strikethrough={htmlHasStrikethrough}");
			});

			void OnHandlerChanged(object sender, EventArgs args)
			{
				if (htmlLabel.Handler?.PlatformView is WTextBlock textBlock)
				{
					htmlLabel.HandlerChanged -= OnHandlerChanged;
					textBlock.Loaded += OnNativeLoaded;
				}
			}

			void OnNativeLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs args)
			{
				var textBlock = (WTextBlock)sender;
				textBlock.Loaded -= OnNativeLoaded;
				nativeLoaded = true;
				nativeLoadedCompletion.TrySetResult();
			}
		}

		static bool HasDecoration(WInlineCollection inlines, WTextDecorations decoration)
		{
			foreach (WInline inline in inlines)
			{
				if ((inline.TextDecorations & decoration) != 0)
					return true;

				if (inline is WSpan span && HasDecoration(span.Inlines, decoration))
					return true;
			}

			return false;
		}
	}
}
#endif

