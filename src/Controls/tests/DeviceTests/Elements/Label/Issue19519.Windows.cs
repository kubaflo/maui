#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using MWindow = Microsoft.Maui.Controls.Window;
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
		public async Task HtmlTextDecorationsAreAppliedToTargetRuns()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<MWindow, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var affectedLabel = new Label
			{
				AutomationId = "HtmlLabel",
				TextType = TextType.Html,
				Text = """
					<p>Este es el primer párrafo con <span style="text-decoration: underline;">texto subrayado</span> para demostrar la funcionalidad.</p>
					<p>Y este es el segundo párrafo, donde parte del <span style="text-decoration: line-through;">texto está tachado</span> para ilustrar otro estilo.</p>
					"""
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 20,
					Children =
					{
						new Label
						{
							Text = "HTML text-decoration on Windows",
							FontSize = 24,
							FontAttributes = FontAttributes.Bold
						},
						affectedLabel,
						new Button
						{
							AutomationId = "CheckHtmlStylingButton",
							Text = "Check HTML styling"
						},
						new Label
						{
							AutomationId = "StylingResult",
							Text = "Styling result",
							FontAttributes = FontAttributes.Bold
						}
					}
				}
			};

			const string underlineText = "texto subrayado";
			const string strikethroughText = "texto está tachado";
			var attachmentObserved = false;
			var underlineRunObserved = false;
			var strikethroughRunObserved = false;
			var observedUnderlineDecorations = (WTextDecorations)4;
			var observedStrikethroughDecorations = (WTextDecorations)4;

			await AttachAndRun(page, _ =>
			{
				var labelHandler = affectedLabel.Handler as LabelHandler;
				Assert.NotNull(labelHandler);

				WTextBlock platformView = labelHandler.PlatformView;
				Assert.NotNull(platformView);

				underlineRunObserved = TryGetDecorations(
					platformView.Inlines,
					underlineText,
					WTextDecorations.None,
					out observedUnderlineDecorations);
				strikethroughRunObserved = TryGetDecorations(
					platformView.Inlines,
					strikethroughText,
					WTextDecorations.None,
					out observedStrikethroughDecorations);

				attachmentObserved = true;
			});

			Assert.True(attachmentObserved, "The LabelHandler attachment callback was not observed.");
			Assert.True(underlineRunObserved, $"The expected HTML run '{underlineText}' was not found.");
			Assert.True(strikethroughRunObserved, $"The expected HTML run '{strikethroughText}' was not found.");
			Assert.True(
				(observedUnderlineDecorations & WTextDecorations.Underline) == WTextDecorations.Underline,
				$"Issue19519 HTML text decoration missing: target '{underlineText}', observed '{observedUnderlineDecorations}', expected '{WTextDecorations.Underline}'.");
			Assert.True(
				(observedStrikethroughDecorations & WTextDecorations.Strikethrough) == WTextDecorations.Strikethrough,
				$"Issue19519 HTML text decoration missing: target '{strikethroughText}', observed '{observedStrikethroughDecorations}', expected '{WTextDecorations.Strikethrough}'.");
		}

		static bool TryGetDecorations(
			WInlineCollection inlines,
			string targetText,
			WTextDecorations inheritedDecorations,
			out WTextDecorations decorations)
		{
			foreach (var inline in inlines)
			{
				var effectiveDecorations = inheritedDecorations | inline.TextDecorations;

				if (inline is WRun run &&
					run.Text.Contains(targetText, StringComparison.Ordinal))
				{
					decorations = effectiveDecorations;
					return true;
				}

				if (inline is WSpan span &&
					TryGetDecorations(span.Inlines, targetText, effectiveDecorations, out decorations))
				{
					return true;
				}
			}

			decorations = WTextDecorations.None;
			return false;
		}
	}
}
#endif

