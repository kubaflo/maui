using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

#if IOS && !MACCATALYST
namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		const double ColorTolerance = 0.01;

		[Fact]
		public async Task NullBackgroundColorRestoresNativeDefault()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
				});
			});

			var cleanImage = CreateImage();
			var cleanImageButton = CreateImageButton();
			Rgba cleanImageDefault = default;
			Rgba cleanImageButtonDefault = default;

			await CreateHandlerAndAddToWindow(CreatePage(cleanImage, cleanImageButton), () =>
			{
				cleanImageDefault = GetRgba(GetNativeView(cleanImage).BackgroundColor);
				cleanImageButtonDefault = GetRgba(GetNativeView(cleanImageButton).BackgroundColor);

				Assert.True(
					cleanImageDefault.Alpha <= ColorTolerance,
					$"Expected the clean Image native default to be transparent, but it was {cleanImageDefault}.");
				Assert.True(
					cleanImageButtonDefault.Alpha <= ColorTolerance,
					$"Expected the clean ImageButton native default to be transparent, but it was {cleanImageButtonDefault}.");
			});

			var image = CreateImage();
			var imageButton = CreateImageButton();
			image.BackgroundColor = Colors.Blue;
			imageButton.BackgroundColor = Colors.Blue;

			var imageTransitions = -1;
			var imageButtonTransitions = -1;
			image.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
					imageTransitions++;
			};
			imageButton.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
					imageButtonTransitions++;
			};

			await CreateHandlerAndAddToWindow(CreatePage(image, imageButton), async () =>
			{
				var nativeImage = GetNativeView(image);
				var nativeImageButton = GetNativeView(imageButton);

				image.BackgroundColor = Colors.Red;
				imageButton.BackgroundColor = Colors.Red;

				Assert.Equal(0, imageTransitions);
				Assert.Equal(0, imageButtonTransitions);
				await AssertEventually(
					() => IsOpaqueRed(nativeImage.BackgroundColor),
					message: "Image native background did not become opaque red.");
				await AssertEventually(
					() => IsOpaqueRed(nativeImageButton.BackgroundColor),
					message: "ImageButton native background did not become opaque red.");

				image.BackgroundColor = null;
				imageButton.BackgroundColor = null;

				Assert.Equal(1, imageTransitions);
				Assert.Equal(1, imageButtonTransitions);
				Assert.Null(image.BackgroundColor);
				Assert.Null(imageButton.BackgroundColor);

				AssertBackgroundMatchesDefault("Image", nativeImage, cleanImageDefault);
				AssertBackgroundMatchesDefault("ImageButton", nativeImageButton, cleanImageButtonDefault);
			});
		}

		static Image CreateImage() => new()
		{
			Source = "dotnet_bot.png",
			Aspect = Aspect.AspectFit,
			HeightRequest = 100
		};

		static ImageButton CreateImageButton() => new()
		{
			Source = "dotnet_bot.png",
			Aspect = Aspect.AspectFit,
			HeightRequest = 100
		};

		static ContentPage CreatePage(Image image, ImageButton imageButton) => new()
		{
			Content = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 8,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
						Text = "Image background reset"
					},
					new Label { Text = "Image" },
					image,
					new Label { Text = "ImageButton" },
					imageButton
				}
			}
		};

		static UIView GetNativeView(VisualElement control)
		{
			Assert.NotNull(control.Handler);
			Assert.NotNull(control.Handler.PlatformView);
			return Assert.IsAssignableFrom<UIView>(control.Handler.PlatformView);
		}

		static bool IsOpaqueRed(UIColor color)
		{
			var rgba = GetRgba(color);
			return Math.Abs(rgba.Red - 1) <= ColorTolerance &&
				rgba.Green <= ColorTolerance &&
				rgba.Blue <= ColorTolerance &&
				Math.Abs(rgba.Alpha - 1) <= ColorTolerance;
		}

		static void AssertBackgroundMatchesDefault(string control, UIView nativeView, Rgba expected)
		{
			var observed = GetRgba(nativeView.BackgroundColor);
			var matches = Math.Abs(observed.Red - expected.Red) <= ColorTolerance &&
				Math.Abs(observed.Green - expected.Green) <= ColorTolerance &&
				Math.Abs(observed.Blue - expected.Blue) <= ColorTolerance &&
				Math.Abs(observed.Alpha - expected.Alpha) <= ColorTolerance;

			Assert.True(
				matches,
				$"Native background should restore its captured default after BackgroundColor was set to null; control={control}; observed={observed}; expected={expected}.");
		}

		static Rgba GetRgba(UIColor color)
		{
			if (color is null)
				return new Rgba(0, 0, 0, 0);

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return new Rgba((double)red, (double)green, (double)blue, (double)alpha);
		}

		readonly record struct Rgba(double Red, double Green, double Blue, double Alpha);
	}
}
#endif

