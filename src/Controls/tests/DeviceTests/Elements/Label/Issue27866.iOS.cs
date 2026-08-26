#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27866")]
	public class Issue27866 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HtmlListsIncludeUnorderedAndOrderedMarkers()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(18))
				return;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			const string html = "<ul><li>item 1</li><li>item 2</li><li>item 3</li></ul><ol><li>item 1</li><li>item 2</li><li>item 3</li></ol>";
			const string unobserved = "Native text was not observed";
			var observedNativeText = unobserved;
			var callbackInvoked = false;
			var label = new Label
			{
				TextType = TextType.Html,
				Text = html,
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				VerticalOptions = LayoutOptions.Center,
				Children = { label },
			};
			var page = new ContentPage
			{
				Content = layout,
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, _ =>
			{
				Assert.NotNull(label.Handler);
				Assert.NotNull(label.Handler.PlatformView);
				var platformLabel = Assert.IsAssignableFrom<UILabel>(label.Handler.PlatformView);
				callbackInvoked = true;
				observedNativeText = platformLabel.AttributedText?.Value ?? string.Empty;
			});

			Assert.True(callbackInvoked, "The post-attachment callback did not run.");
			Assert.NotEqual(unobserved, observedNativeText);

			foreach (var item in new[] { "item 1", "item 2", "item 3" })
			{
				var count = CountOccurrences(observedNativeText, item);
				Assert.True(count == 2, $"Expected '{item}' twice in the native attributed text, but found {count}. Native text: {observedNativeText}");
			}

			var hasBullets = observedNativeText.Contains("•", StringComparison.Ordinal);
			var hasNumbers =
				observedNativeText.Contains("1.", StringComparison.Ordinal) &&
				observedNativeText.Contains("2.", StringComparison.Ordinal) &&
				observedNativeText.Contains("3.", StringComparison.Ordinal);

			Assert.True(
				hasBullets && hasNumbers,
				$"iOS HTML list markers were missing from the native attributed text. Bullets: {hasBullets}; numbers: {hasNumbers}; native text: {observedNativeText}");
		}

		static int CountOccurrences(string text, string value)
		{
			var count = 0;
			var index = 0;

			while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
			{
				count++;
				index += value.Length;
			}

			return count;
		}
	}
}
#endif

