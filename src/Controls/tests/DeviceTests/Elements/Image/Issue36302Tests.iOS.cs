#if MACCATALYST
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task NullBackgroundColorRestoresTransparentNativeBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			Rgba cleanImageBackground = default;
			Rgba cleanImageButtonBackground = default;
			var cleanImage = new Image();
			var cleanImageButton = new ImageButton();
			var cleanGrid = CreateTwoColumnGrid(cleanImage, cleanImageButton);
			var cleanPage = new ContentPage { Content = cleanGrid };

			await CreateHandlerAndAddToWindow(cleanPage, () =>
			{
				var cleanImageView = Assert.IsAssignableFrom<UIImageView>(cleanImage.Handler.PlatformView);
				var cleanImageButtonView = Assert.IsType<UIButton>(cleanImageButton.Handler.PlatformView);
				cleanImageBackground = ReadBackground(cleanImageView);
				cleanImageButtonBackground = ReadBackground(cleanImageButtonView);

				Assert.True(CloseEnough(0, cleanImageBackground.Alpha),
					$"Clean Image background was not transparent: {cleanImageBackground}");
				Assert.True(CloseEnough(0, cleanImageButtonBackground.Alpha),
					$"Clean ImageButton background was not transparent: {cleanImageButtonBackground}");
			});

			var image = new Image
			{
				Source = "dotnet_bot.png",
				BackgroundColor = Colors.Green,
				HeightRequest = 160,
				Aspect = Aspect.AspectFit
			};
			var imageButton = new ImageButton
			{
				Source = "dotnet_bot.png",
				BackgroundColor = Colors.Green,
				HeightRequest = 160,
				Aspect = Aspect.AspectFit
			};
			var setRedButton = new Button { Text = "Set backgrounds to red" };
			var setNullButton = new Button { Text = "Set backgrounds to null" };

			Color imageRedObserved = Colors.Magenta;
			Color imageButtonRedObserved = Colors.Magenta;
			Color imageNullObserved = Colors.Magenta;
			Color imageButtonNullObserved = Colors.Magenta;
			var imageRedChanged = new TaskCompletionSource();
			var imageButtonRedChanged = new TaskCompletionSource();
			var imageNullChanged = new TaskCompletionSource();
			var imageButtonNullChanged = new TaskCompletionSource();

			void OnImageBackgroundChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName != nameof(VisualElement.BackgroundColor))
					return;

				if (image.BackgroundColor == Colors.Red)
				{
					imageRedObserved = image.BackgroundColor;
					imageRedChanged.TrySetResult();
				}
				else if (image.BackgroundColor is null)
				{
					imageNullObserved = image.BackgroundColor;
					imageNullChanged.TrySetResult();
				}
			}

			void OnImageButtonBackgroundChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName != nameof(VisualElement.BackgroundColor))
					return;

				if (imageButton.BackgroundColor == Colors.Red)
				{
					imageButtonRedObserved = imageButton.BackgroundColor;
					imageButtonRedChanged.TrySetResult();
				}
				else if (imageButton.BackgroundColor is null)
				{
					imageButtonNullObserved = imageButton.BackgroundColor;
					imageButtonNullChanged.TrySetResult();
				}
			}

			image.PropertyChanged += OnImageBackgroundChanged;
			imageButton.PropertyChanged += OnImageButtonBackgroundChanged;
			setRedButton.Clicked += (sender, args) =>
			{
				image.BackgroundColor = Colors.Red;
				imageButton.BackgroundColor = Colors.Red;
			};
			setNullButton.Clicked += (sender, args) =>
			{
				image.BackgroundColor = null;
				imageButton.BackgroundColor = null;
			};

			var grid = CreateTwoColumnGrid(image, imageButton);
			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					grid,
					setRedButton,
					setNullButton
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = stack }
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(image.Source);
				Assert.NotNull(imageButton.Source);
				Assert.EndsWith("dotnet_bot.png", image.Source.ToString(), StringComparison.Ordinal);
				Assert.EndsWith("dotnet_bot.png", imageButton.Source.ToString(), StringComparison.Ordinal);
				Assert.Equal(160, image.HeightRequest);
				Assert.Equal(160, imageButton.HeightRequest);
				Assert.Equal(Aspect.AspectFit, image.Aspect);
				Assert.Equal(Aspect.AspectFit, imageButton.Aspect);
				Assert.Equal(Colors.Green, image.BackgroundColor);
				Assert.Equal(Colors.Green, imageButton.BackgroundColor);

				Assert.IsType<ImageHandler>(image.Handler);
				Assert.IsType<ImageButtonHandler>(imageButton.Handler);
				var imageView = Assert.IsAssignableFrom<UIImageView>(image.Handler.PlatformView);
				var imageButtonView = Assert.IsType<UIButton>(imageButton.Handler.PlatformView);
				var redButtonView = Assert.IsType<UIButton>(setRedButton.Handler.PlatformView);
				var nullButtonView = Assert.IsType<UIButton>(setNullButton.Handler.PlatformView);

				Assert.True(IsOpaqueGreen(ReadBackground(imageView)), "Image did not begin with an opaque green native background.");
				Assert.True(IsOpaqueGreen(ReadBackground(imageButtonView)), "ImageButton did not begin with an opaque green native background.");

				redButtonView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await Task.WhenAll(imageRedChanged.Task, imageButtonRedChanged.Task).WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(Colors.Red, imageRedObserved);
				Assert.Equal(Colors.Red, imageButtonRedObserved);
				await AssertHelpers.AssertEventually(
					() => IsOpaqueRed(ReadBackground(imageView)) && IsOpaqueRed(ReadBackground(imageButtonView)),
					timeout: 2000,
					message: "Image and ImageButton native backgrounds did not become opaque red.");

				nullButtonView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await Task.WhenAll(imageNullChanged.Task, imageButtonNullChanged.Task).WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Null(imageNullObserved);
				Assert.Null(imageButtonNullObserved);

				Rgba imageAfterNull = default;
				Rgba imageButtonAfterNull = default;
				await AssertHelpers.Wait(() =>
				{
					imageAfterNull = ReadBackground(imageView);
					imageButtonAfterNull = ReadBackground(imageButtonView);
					return CloseEnough(cleanImageBackground.Alpha, imageAfterNull.Alpha) &&
						CloseEnough(cleanImageButtonBackground.Alpha, imageButtonAfterNull.Alpha);
				}, timeout: 2000);

				var failureMessage =
					$"BackgroundColor null reset failed: Image actual {imageAfterNull}, expected alpha {cleanImageBackground.Alpha:F3}; " +
					$"ImageButton actual {imageButtonAfterNull}, expected alpha {cleanImageButtonBackground.Alpha:F3}.";
				Assert.True(CloseEnough(cleanImageBackground.Alpha, imageAfterNull.Alpha), failureMessage);
				Assert.True(CloseEnough(cleanImageButtonBackground.Alpha, imageButtonAfterNull.Alpha), failureMessage);
			});
		}

		static Grid CreateTwoColumnGrid(View first, View second)
		{
			var grid = new Grid
			{
				ColumnSpacing = 16,
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};
			Grid.SetColumn(second, 1);
			grid.Add(first);
			grid.Add(second);
			return grid;
		}

		static Rgba ReadBackground(UIView view)
		{
			if (view.BackgroundColor is not UIColor color)
				return default;

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return new Rgba((double)red, (double)green, (double)blue, (double)alpha);
		}

		static bool IsOpaqueGreen(Rgba color) =>
			color.Red < 0.01 && color.Green > 0.4 && color.Blue < 0.01 && color.Alpha > 0.99;

		static bool IsOpaqueRed(Rgba color) =>
			color.Red > 0.99 && color.Green < 0.01 && color.Blue < 0.01 && color.Alpha > 0.99;

		static bool CloseEnough(double expected, double actual) =>
			Math.Abs(expected - actual) <= 0.01;

		readonly record struct Rgba(double Red, double Green, double Blue, double Alpha)
		{
			public override string ToString() =>
				$"RGBA({Red:F3}, {Green:F3}, {Blue:F3}, {Alpha:F3})";
		}
	}
}
#endif

