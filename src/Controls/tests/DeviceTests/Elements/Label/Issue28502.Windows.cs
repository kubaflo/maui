using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28502")]
	public class Issue28502 : ControlsHandlerTestBase
	{
		const string RequestedFont = "OpenSansRegular";

		[Fact]
		public async Task UnregisteredFontAliasDoesNotResolveToMissingPackagedAssets()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var defaultLabel = new Label
			{
				Text = "Default font sample"
			};

			var fontHost = new VerticalStackLayout
			{
				Spacing = 8,
				Children = { defaultLabel }
			};

			var rootLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children = { fontHost }
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = rootLayout
				}
			};

			var handlerChangedCount = 0;
			string observedSource = null;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Same(fontHost, defaultLabel.Parent);
				Assert.NotNull(defaultLabel.Handler);

				var defaultTextBlock = Assert.IsType<WTextBlock>(defaultLabel.Handler.PlatformView);
				Assert.NotNull(defaultTextBlock.FontFamily);
				Assert.DoesNotContain($"Assets/Fonts/{RequestedFont}", defaultTextBlock.FontFamily.Source, StringComparison.Ordinal);

				var sourceCompletion = new TaskCompletionSource<string>();
				var affectedLabel = new Label
				{
					Text = "OpenSansRegular sample text",
					FontFamily = RequestedFont,
					FontSize = 28
				};

				affectedLabel.HandlerChanged += OnAffectedLabelHandlerChanged;
				fontHost.Add(affectedLabel);

				observedSource = await sourceCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				affectedLabel.HandlerChanged -= OnAffectedLabelHandlerChanged;

				Assert.True(handlerChangedCount > 0, "The dynamically inserted Label did not raise HandlerChanged.");
				Assert.Same(fontHost, affectedLabel.Parent);
				Assert.Equal(RequestedFont, affectedLabel.FontFamily);
				Assert.NotNull(affectedLabel.Handler);

				var affectedTextBlock = Assert.IsType<WTextBlock>(affectedLabel.Handler.PlatformView);
				Assert.Equal(observedSource, affectedTextBlock.FontFamily.Source);

				void OnAffectedLabelHandlerChanged(object sender, EventArgs args)
				{
					if (!ReferenceEquals(sender, affectedLabel))
						return;

					handlerChangedCount++;
					if (affectedLabel.Handler?.PlatformView is WTextBlock textBlock)
						sourceCompletion.TrySetResult(textBlock.FontFamily.Source);
				}
			});

			Assert.NotNull(observedSource);
			Assert.True(
				string.Equals(RequestedFont, observedSource, StringComparison.Ordinal),
				$"Unregistered font alias resolved to missing packaged assets. Observed: '{observedSource}'. Expected: '{RequestedFont}'.");
		}
	}
}

