#if !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingBackgroundColorRestoresNativeTransparentDefault()
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
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
				});
			});

			var (page, imageButton, setRedButton, clearButton) = CreatePage();

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(imageButton.Source);

				var nativeButton = Assert.IsType<UIButton>(imageButton.Handler.PlatformView);
				var nativeSetRedButton = Assert.IsType<UIButton>(setRedButton.Handler.PlatformView);
				var nativeClearButton = Assert.IsType<UIButton>(clearButton.Handler.PlatformView);
				await AssertEventually(
					() => ColorsMatch(nativeButton.BackgroundColor, Colors.Blue),
					message: $"Initial native ImageButton background was {DescribeColor(nativeButton.BackgroundColor)} instead of blue.");

				nativeSetRedButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(
					() => ColorsMatch(nativeButton.BackgroundColor, Colors.Red),
					message: $"Native ImageButton background was {DescribeColor(nativeButton.BackgroundColor)} after setting red.");

				int transitionSentinel = -1;
				imageButton.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == ImageButton.BackgroundColorProperty.PropertyName)
						transitionSentinel = imageButton.BackgroundColor is null ? 1 : 0;
				};

				nativeClearButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(
					() => transitionSentinel == 1,
					message: "ImageButton did not raise PropertyChanged after BackgroundColor was cleared.");
				Assert.Equal(1, transitionSentinel);
				Assert.Null(imageButton.BackgroundColor);

				var nativeButtonAfterClear = Assert.IsType<UIButton>(imageButton.Handler.PlatformView);
				await AssertEventually(
					() => GetAlpha(nativeButtonAfterClear.BackgroundColor) <= 0.001,
					message: $"ImageButton native background remained opaque after BackgroundColor was cleared: actual {DescribeColor(nativeButtonAfterClear.BackgroundColor)}.");
			});
		}

		static (ContentPage Page, ImageButton ImageButton, Button SetRedButton, Button ClearButton) CreatePage()
		{
			var imageButton = new ImageButton
			{
				AutomationId = "AffectedImageButton",
				Source = "dotnet_bot.png",
				WidthRequest = 140,
				HeightRequest = 140,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				BackgroundColor = Colors.Blue,
			};

			var setRedButton = new Button
			{
				AutomationId = "SetRedButton",
				Text = "Set Background Red",
			};
			setRedButton.Clicked += (_, _) => imageButton.BackgroundColor = Colors.Red;

			var clearButton = new Button
			{
				AutomationId = "ClearButton",
				Text = "Clear Background",
			};
			clearButton.Clicked += (_, _) => imageButton.BackgroundColor = null;

			var imageContainer = new Grid
			{
				HeightRequest = 170,
				BackgroundColor = Colors.LightGray,
			};
			imageContainer.Add(imageButton);

			var content = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 10,
				Children =
				{
					new Label
					{
						AutomationId = "TitleLabel",
						Text = "ImageButton BackgroundColor reset",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "The control below is the affected ImageButton. It starts blue, changes to red, and should become transparent when cleared.",
						FontSize = 16,
					},
					imageContainer,
					new Label
					{
						AutomationId = "StateLabel",
						Text = "Reference state: Blue initial background",
						FontSize = 16,
					},
					setRedButton,
					clearButton,
					new Label
					{
						AutomationId = "InstructionsLabel",
						Text = "Use the buttons above to change and clear the background.",
						FontSize = 18,
						FontAttributes = FontAttributes.Bold,
					},
				},
			};

			return (new ContentPage
			{
				Title = "Image background reset",
				Content = new ScrollView { Content = content },
			}, imageButton, setRedButton, clearButton);
		}

		static bool ColorsMatch(UIColor nativeColor, Color expected)
		{
			if (nativeColor is null)
				return false;

			var actual = nativeColor.ToColor();
			return Math.Abs(actual.Red - expected.Red) <= 0.001
				&& Math.Abs(actual.Green - expected.Green) <= 0.001
				&& Math.Abs(actual.Blue - expected.Blue) <= 0.001
				&& Math.Abs(actual.Alpha - expected.Alpha) <= 0.001;
		}

		static double GetAlpha(UIColor nativeColor) =>
			nativeColor is null ? 0 : (double)nativeColor.CGColor.Alpha;

		static string DescribeColor(UIColor nativeColor)
		{
			if (nativeColor is null)
				return "rgba(0,0,0,0)";

			var color = nativeColor.ToColor();
			return FormattableString.Invariant($"rgba({color.Red:0.###},{color.Green:0.###},{color.Blue:0.###},{color.Alpha:0.###})");
		}
	}
}
#endif

