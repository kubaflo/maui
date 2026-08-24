#if !MACCATALYST
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingBackgroundColorRestoresNativeDefault()
		{
			EnsureHandlerCreated(builder =>
				builder.ConfigureMauiHandlers(handlers => handlers.AddMauiControlsHandlers()));

			var cleanImage = CreateImage(null);
			UIColor cleanDefaultBackground = null;

			await CreateHandlerAndAddToWindow(CreateRecordedPage(cleanImage), () =>
			{
				var cleanHandler = Assert.IsType<ImageHandler>(cleanImage.Handler);
				var cleanPlatformImage = Assert.IsAssignableFrom<UIImageView>(cleanHandler.PlatformView);

				Assert.True(IsNullOrTransparent(cleanPlatformImage.BackgroundColor));
				cleanDefaultBackground = cleanPlatformImage.BackgroundColor;
			});

			var affectedImage = CreateImage(Colors.Blue);
			var affectedPage = CreateRecordedPage(affectedImage);

			await CreateHandlerAndAddToWindow(affectedPage, async () =>
			{
				Assert.NotNull(affectedImage.Source);
				Assert.Equal(180, affectedImage.HeightRequest);
				Assert.Equal(Aspect.AspectFit, affectedImage.Aspect);

				var imageHandler = Assert.IsType<ImageHandler>(affectedImage.Handler);
				var platformImage = Assert.IsAssignableFrom<UIImageView>(imageHandler.PlatformView);

				await AssertHelpers.AssertEventually(
					() => platformImage.Frame.Width > 0 && platformImage.Frame.Height > 0,
					message: "The native Image must have a nonempty frame.");
				await AssertHelpers.AssertEventually(
					() => HasColor(platformImage.BackgroundColor, Colors.Blue),
					message: "The native Image background did not become blue.");

				affectedImage.BackgroundColor = Colors.Red;

				await AssertHelpers.AssertEventually(
					() => HasColor(platformImage.BackgroundColor, Colors.Red),
					message: "The native Image background did not become red.");
				Assert.Same(platformImage, Assert.IsType<ImageHandler>(affectedImage.Handler).PlatformView);

				var clearTransition = -1;
				void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == Image.BackgroundColorProperty.PropertyName)
						clearTransition = affectedImage.BackgroundColor is null ? 1 : 0;
				}

				affectedImage.PropertyChanged += OnPropertyChanged;
				affectedImage.BackgroundColor = null;
				affectedImage.PropertyChanged -= OnPropertyChanged;

				Assert.Equal(1, clearTransition);
				Assert.Null(affectedImage.BackgroundColor);
				Assert.Same(platformImage, Assert.IsType<ImageHandler>(affectedImage.Handler).PlatformView);

				var measuredBackground = "not measured";
				var restoredDefault = await AssertHelpers.Wait(() =>
				{
					measuredBackground = FormatBackground(platformImage.BackgroundColor);
					return MatchesDefault(platformImage.BackgroundColor, cleanDefaultBackground);
				});

				Assert.True(
					restoredDefault,
					$"Image native BackgroundColor after BackgroundColor=null was {measuredBackground}; expected captured default null/transparent.");
			});
		}

		static Image CreateImage(Color backgroundColor) =>
			new Image
			{
				Source = "dotnet_bot.png",
				BackgroundColor = backgroundColor,
				HeightRequest = 180,
				Aspect = Aspect.AspectFit
			};

		static ContentPage CreateRecordedPage(Image image)
		{
			var layout = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Issue 36302: BackgroundColor reset",
						FontSize = 22,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "The Image begins blue. Set it red, then clear BackgroundColor.",
						FontSize = 16
					},
					new Label
					{
						Text = "Image",
						HorizontalTextAlignment = TextAlignment.Center
					},
					image,
					new Label
					{
						Text = "Current managed BackgroundColor",
						FontSize = 18,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "BackgroundColor transition result",
						FontSize = 18,
						FontAttributes = FontAttributes.Bold
					},
					new Button { Text = "Set Background Red" },
					new Button { Text = "Clear Background" }
				}
			};

			return new ContentPage
			{
				Content = new ScrollView { Content = layout }
			};
		}

		static bool HasColor(UIColor actual, Color expected) =>
			actual is not null && expected.Equals(actual.ToColor());

		static bool MatchesDefault(UIColor actual, UIColor expected)
		{
			if (IsNullOrTransparent(expected))
				return IsNullOrTransparent(actual);

			return actual is not null && Equals(actual.ToColor(), expected.ToColor());
		}

		static bool IsNullOrTransparent(UIColor color)
		{
			if (color is null)
				return true;

			var managedColor = color.ToColor();
			return managedColor is null || managedColor.Alpha == 0;
		}

		static string FormatBackground(UIColor color)
		{
			if (color is null)
				return "null/transparent";

			var managedColor = color.ToColor();
			if (managedColor is null)
				return "null/transparent";

			return string.Format(
				CultureInfo.InvariantCulture,
				"RGBA({0:F3},{1:F3},{2:F3},{3:F3})",
				managedColor.Red,
				managedColor.Green,
				managedColor.Blue,
				managedColor.Alpha);
		}
	}
}
#endif

