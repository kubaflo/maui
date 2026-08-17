#if !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Button)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36749 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CreationTimeNativeColorsArePreservedWhenMauiColorsAreUnset()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<NativeStyledButton, NativeStyledButtonHandler>();
				});
			});

			var setupButton = new Button
			{
				Text = "Show custom button",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
			var page = new ContentPage { Content = setupButton };
			var window = new Window(page);
			var customButton = new NativeStyledButton { Text = "Native styled button" };

			Assert.Null(customButton.Style);
			Assert.True(Brush.IsNullOrEmpty(customButton.Background));
			Assert.Null(customButton.BackgroundColor);
			Assert.Null(customButton.TextColor);

			bool handlerConnected = false;
			UIColor observedBackground = UIColor.Magenta;
			UIColor observedTitleColor = UIColor.Magenta;

			await CreateHandlerAndAddToWindow<IWindowHandler>(window, async _ =>
			{
				page.Content = customButton;
				await OnLoadedAsync(customButton);

				Assert.Same(customButton, page.Content);
				var customHandler = Assert.IsType<NativeStyledButtonHandler>(customButton.Handler);
				var platformButton = customHandler.PlatformView;
				Assert.Same(customHandler.CreatedPlatformView, platformButton);
				Assert.NotNull(platformButton.Window);

				observedBackground = platformButton.BackgroundColor;
				observedTitleColor = platformButton.TitleColor(UIControlState.Normal);
				handlerConnected = true;
			});

			Assert.True(handlerConnected, "The custom button handler should connect after replacing the attached page content.");
			Assert.True(
				ColorComparison.ARGBEquivalent(UIColor.Cyan, observedBackground),
				"The custom UIButton creation-time cyan background should be preserved when MAUI Background is unset.");
			Assert.True(
				ColorComparison.ARGBEquivalent(UIColor.DarkGray, observedTitleColor),
				"The custom UIButton creation-time dark-gray title color should be preserved when MAUI TextColor is unset.");
		}

		public sealed class NativeStyledButton : Button
		{
		}

		public sealed class NativeStyledButtonHandler : ButtonHandler
		{
			public NativeStyledPlatformButton CreatedPlatformView { get; private set; }

			protected override UIButton CreatePlatformView()
			{
				CreatedPlatformView = NativeStyledPlatformButton.CreateWithNativeStyle();
				return CreatedPlatformView;
			}
		}

		public sealed class NativeStyledPlatformButton : UIButton
		{
			public static NativeStyledPlatformButton CreateWithNativeStyle()
			{
				var button = new NativeStyledPlatformButton
				{
					BackgroundColor = UIColor.Cyan,
				};
				button.SetTitleColor(UIColor.DarkGray, UIControlState.Normal);
				return button;
			}
		}
	}
}
#endif
