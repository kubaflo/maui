#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37540")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DynamicResourceUpdatesLabelBackgroundAfterLoaded()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var resourceColor = Colors.Red;
			var affectedLabel = new Label
			{
				Background = new SolidColorBrush(Colors.Transparent),
				FontSize = 20,
				Padding = 20,
				Text = "Affected label",
				TextColor = Colors.Black
			};
			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				RowSpacing = 20
			};
			grid.Add(new Label
			{
				Text = "The label below should have a red background after the page loads."
			}, 0, 0);
			grid.Add(affectedLabel, 0, 1);
			grid.Add(new Button
			{
				IsVisible = false,
				Text = "Check background"
			}, 0, 2);
			grid.Add(new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Text = string.Empty,
				VerticalOptions = LayoutOptions.Start
			}, 0, 3);

			var page = new ContentPage
			{
				Content = grid
			};
			page.Resources["backgroundColor"] = resourceColor;

			var loadedSentinel = -1;
			UILabel loadedNativeLabel = null;
			Color initialNativeColor = null;
			page.Loaded += (_, _) =>
			{
				loadedNativeLabel = affectedLabel.Handler?.PlatformView as UILabel;
				if (loadedNativeLabel is not null)
					initialNativeColor = loadedNativeLabel.BackgroundColor?.ToColor();

				loadedSentinel = 1;
				affectedLabel.SetDynamicResource(Label.BackgroundProperty, "backgroundColor");
			};

			Assert.Same(resourceColor, page.Resources["backgroundColor"]);
			var initialBrush = Assert.IsType<SolidColorBrush>(affectedLabel.Background);
			Assert.Equal(Colors.Transparent, initialBrush.Color);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertionExtensions.AssertEventually(
					() => loadedSentinel == 1,
					message: "ContentPage Loaded did not run.");
				Assert.Equal(1, loadedSentinel);

				var platformPage = page.Handler?.PlatformView as UIView;
				var affectedNativeLabel = affectedLabel.Handler?.PlatformView as UILabel;
				Assert.NotNull(platformPage);
				Assert.NotNull(affectedNativeLabel);
				Assert.NotNull(loadedNativeLabel);
				Assert.Same(affectedNativeLabel, loadedNativeLabel);
				Assert.True(initialNativeColor is null || AreClose(Colors.Transparent, initialNativeColor),
					$"Label did not begin transparent. Observed: {FormatColor(initialNativeColor)}");

				Color observedNativeColor = null;
				await AssertionExtensions.AssertEventually(
					() =>
					{
						var nativeColor = affectedNativeLabel.BackgroundColor;
						if (nativeColor is not null)
							observedNativeColor = nativeColor.ToColor();

						return observedNativeColor is not null
							&& AreClose(resourceColor, observedNativeColor);
					},
					message: $"Affected UILabel background did not update to the page's Red dynamic resource after Loaded. Observed: {FormatColor(observedNativeColor)}");
			});
		}

		static bool AreClose(Color expected, Color actual)
		{
			const float tolerance = 0.01f;
			return Math.Abs(expected.Red - actual.Red) <= tolerance
				&& Math.Abs(expected.Green - actual.Green) <= tolerance
				&& Math.Abs(expected.Blue - actual.Blue) <= tolerance
				&& Math.Abs(expected.Alpha - actual.Alpha) <= tolerance;
		}

		static string FormatColor(Color color) =>
			color is null
				? "<null>"
				: $"RGBA({color.Red:F3}, {color.Green:F3}, {color.Blue:F3}, {color.Alpha:F3})";
	}
}
#endif

