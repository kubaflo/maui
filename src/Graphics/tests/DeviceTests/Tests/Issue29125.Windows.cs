using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.Maui.TestUtils.DeviceTests.Runners;
using Xunit;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WThumb = Microsoft.UI.Xaml.Controls.Primitives.Thumb;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
using WWindow = Microsoft.UI.Xaml.Window;

namespace Microsoft.Maui.Graphics.DeviceTests;

[Category("Issue29125")]
public class Issue29125
{
	[Fact]
	public async Task ThumbImageSourceRetainsDefaultThumbSize()
	{
		await TestDispatcher.Current.DispatchAsync(async () =>
		{
			var builder = MauiApp.CreateBuilder()
				.UseMauiApp<Application>()
				.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Slider, SliderHandler>();
				});

			await using var mauiApp = builder.Build();
			var mauiContext = new MauiContext(mauiApp.Services);
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 100,
				Value = 25,
			};
			var measurementLabel = new Label
			{
				Text = "Waiting for the default slider measurement.",
			};
			var button = new Button
			{
				Text = "Set thumb image",
			};
			var resultLabel = new Label
			{
				Text = "Default thumb is ready for comparison.",
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label
						{
							Text = "The slider starts with its default thumb. Set the image and compare the slider's height before and after.",
						},
						slider,
						measurementLabel,
						button,
						resultLabel,
					},
				},
			};

			button.Clicked += (_, _) =>
				slider.ThumbImageSource = "dotnet_bot.png";

			var platformPage = page.ToPlatform(mauiContext);
			var nativeWindow = new WWindow
			{
				Content = platformPage,
			};

			try
			{
				nativeWindow.Activate();

				await new Func<bool>(() =>
					platformPage.IsLoaded &&
					slider.Handler is SliderHandler sliderHandler &&
					FindThumb(sliderHandler.PlatformView) is not null)
					.AssertEventually(timeout: 5000, message: "The Slider thumb did not load.");

				Assert.Null(slider.ThumbImageSource);
				var handler = Assert.IsType<SliderHandler>(slider.Handler);
				var nativeSlider = handler.PlatformView;
				var nativeMauiSlider = Assert.IsType<MauiSlider>(nativeSlider);
				var defaultThumb = Assert.IsType<WThumb>(FindThumb(nativeSlider));
				var expectedWidth = defaultThumb.Width;
				var expectedHeight = defaultThumb.Height;
				const double tolerance = 0.5;

				Assert.True(expectedWidth > 0 && expectedHeight > 0,
					$"The default Slider thumb had invalid dimensions {expectedWidth:F1}x{expectedHeight:F1}.");
				Assert.True(
					Math.Abs(defaultThumb.ActualWidth - expectedWidth) <= tolerance &&
					Math.Abs(defaultThumb.ActualHeight - expectedHeight) <= tolerance,
					$"The default Slider thumb rendered at {defaultThumb.ActualWidth:F1}x{defaultThumb.ActualHeight:F1}, expected {expectedWidth:F1}x{expectedHeight:F1}.");

				var imageSourceChanged = false;
				var callbackToken = nativeMauiSlider.RegisterPropertyChangedCallback(
					MauiSlider.ThumbImageSourceProperty,
					(_, _) => imageSourceChanged = true);

				try
				{
					button.SendClicked();

					await new Func<bool>(() => imageSourceChanged)
						.AssertEventually(timeout: 5000, message: "The native ThumbImageSource property did not change.");
					await new Func<bool>(() =>
						nativeMauiSlider.ThumbImageSource is Microsoft.UI.Xaml.Media.Imaging.BitmapImage image &&
						image.PixelWidth > 0 &&
						image.PixelHeight > 0)
						.AssertEventually(timeout: 5000, message: "The Slider thumb image did not load.");
					await new Func<bool>(() =>
						defaultThumb.ActualWidth > 0 &&
						defaultThumb.ActualHeight > 0)
						.AssertEventually(timeout: 5000, message: "The Slider thumb did not complete layout.");

					var currentThumb = Assert.IsType<WThumb>(FindThumb(nativeSlider));
					Assert.Same(defaultThumb, currentThumb);
					Assert.True(
						Math.Abs(currentThumb.ActualWidth - expectedWidth) <= tolerance &&
						Math.Abs(currentThumb.ActualHeight - expectedHeight) <= tolerance,
						$"Slider thumb image size differed from the default thumb size: observed {currentThumb.ActualWidth:F1}x{currentThumb.ActualHeight:F1}, expected {expectedWidth:F1}x{expectedHeight:F1}.");
				}
				finally
				{
					nativeMauiSlider.UnregisterPropertyChangedCallback(MauiSlider.ThumbImageSourceProperty, callbackToken);
				}
			}
			finally
			{
				nativeWindow.Content = null;
				nativeWindow.Close();
			}
		});
	}

	static WThumb FindThumb(WDependencyObject root)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < childCount; i++)
		{
			var child = WVisualTreeHelper.GetChild(root, i);
			if (child is WThumb thumb)
				return thumb;

			var descendant = FindThumb(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}
}

