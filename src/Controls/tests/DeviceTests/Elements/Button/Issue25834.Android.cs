using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25834")]
	public class Issue25834 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DefaultPaddingDoesNotClipButtonText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var defaultPaddingButton = new NoTabStopButton
			{
				Text = "Settings",
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				FontSize = 18
			};

			var zeroPaddingButton = new NoTabStopButton
			{
				Text = "Settings",
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Padding = 0,
				FontSize = 18
			};

			var defaultPaddingGrid = new Grid
			{
				WidthRequest = 90,
				HeightRequest = 56,
				HorizontalOptions = LayoutOptions.Start
			};
			defaultPaddingGrid.Add(defaultPaddingButton);

			var zeroPaddingGrid = new Grid
			{
				WidthRequest = 90,
				HeightRequest = 56,
				HorizontalOptions = LayoutOptions.Start
			};
			zeroPaddingGrid.Add(zeroPaddingButton);

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Default Android button padding can clip the final letter in Settings.",
						FontSize = 18
					},
					new Label
					{
						Text = "Default padding",
						FontAttributes = FontAttributes.Bold
					},
					defaultPaddingGrid,
					new Label
					{
						Text = "Padding=0 reference",
						FontAttributes = FontAttributes.Bold
					},
					zeroPaddingGrid
				}
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = content
				}
			};

			var defaultSizeChanged = false;
			var zeroSizeChanged = false;
			defaultPaddingButton.SizeChanged += (_, _) => defaultSizeChanged = true;
			zeroPaddingButton.SizeChanged += (_, _) => zeroSizeChanged = true;

			await CreateHandlerAndAddToWindow(page, () =>
			{
				Assert.True(defaultSizeChanged, "The default-padding button did not complete a size change after attachment.");
				Assert.True(zeroSizeChanged, "The Padding=0 button did not complete a size change after attachment.");

				Assert.Same(defaultPaddingGrid, defaultPaddingButton.Parent);
				Assert.Same(zeroPaddingGrid, zeroPaddingButton.Parent);
				Assert.Same(defaultPaddingButton, defaultPaddingGrid.Children[0]);
				Assert.Same(zeroPaddingButton, zeroPaddingGrid.Children[0]);
				Assert.True(defaultPaddingGrid.Y < zeroPaddingGrid.Y, "The default-padding button should be above the Padding=0 reference.");

				var defaultHandler = Assert.IsType<ButtonHandler>(defaultPaddingButton.Handler);
				var zeroHandler = Assert.IsType<ButtonHandler>(zeroPaddingButton.Handler);
				var defaultPlatformView = defaultHandler.PlatformView;
				var zeroPlatformView = zeroHandler.PlatformView;

				Assert.NotNull(defaultPlatformView);
				Assert.NotNull(zeroPlatformView);
				Assert.Equal("Settings", defaultPlatformView.Text);
				Assert.Equal("Settings", zeroPlatformView.Text);
				Assert.True(defaultPlatformView.Width > 0 && defaultPlatformView.Height > 0, "The default-padding native button was not laid out.");
				Assert.True(zeroPlatformView.Width > 0 && zeroPlatformView.Height > 0, "The Padding=0 native button was not laid out.");

				var defaultWidthInDips = defaultPlatformView.Context.FromPixels(defaultPlatformView.Width);
				var zeroWidthInDips = zeroPlatformView.Context.FromPixels(zeroPlatformView.Width);
				var onePixelInDips = defaultPlatformView.Context.FromPixels(1);
				Assert.InRange(defaultWidthInDips, 90d - onePixelInDips, 90d + onePixelInDips);
				Assert.InRange(zeroWidthInDips, 90d - onePixelInDips, 90d + onePixelInDips);

				var defaultTextWidth = defaultPlatformView.Paint.MeasureText(defaultPlatformView.Text);
				var zeroTextWidth = zeroPlatformView.Paint.MeasureText(zeroPlatformView.Text);
				var defaultCompoundPadding = defaultPlatformView.CompoundPaddingLeft + defaultPlatformView.CompoundPaddingRight;
				var zeroCompoundPadding = zeroPlatformView.CompoundPaddingLeft + zeroPlatformView.CompoundPaddingRight;
				var defaultContentWidth = defaultPlatformView.Width - defaultCompoundPadding;
				var zeroContentWidth = zeroPlatformView.Width - zeroCompoundPadding;
				const float fitTolerance = 0.5f;

				Assert.True(
					zeroContentWidth + fitTolerance >= zeroTextWidth,
					$"Padding=0 reference should fit text: text width {zeroTextWidth}px, content width {zeroContentWidth}px, compound padding {zeroCompoundPadding}px.");
				Assert.True(
					defaultContentWidth + fitTolerance >= defaultTextWidth,
					$"Issue25834 default-padding button clips text: expected content width to be at least text width; text width {defaultTextWidth}px, content width {defaultContentWidth}px, compound padding {defaultCompoundPadding}px.");
			});
		}

		public class NoTabStopButton : Button
		{
		}
	}
}

