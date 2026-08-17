#if !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Button)]
	public class Issue21488 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CharacterSpacedButtonUpdatesNativeAttributedTitleAfterClick()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			const string initialText = "Click me";
			const string updatedText = "Clicked 1 time";
			bool clickObserved = false;

			var targetButton = new Button
			{
				Text = initialText,
				CharacterSpacing = 2,
				HorizontalOptions = LayoutOptions.Fill
			};

			targetButton.Clicked += (_, _) =>
			{
				clickObserved = true;
				targetButton.Text = updatedText;
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 30,
						Spacing = 24,
						VerticalOptions = LayoutOptions.Center,
						Children =
						{
							new Label
							{
								Text = "Button CharacterSpacing text update",
								FontSize = 20,
								HorizontalOptions = LayoutOptions.Center
							},
							new Button { Text = "Reset" },
							targetButton,
							new Button { Text = "Record visual result" },
							new Label
							{
								Text = "NO BUG:",
								FontSize = 18,
								HorizontalOptions = LayoutOptions.Center
							}
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<ButtonHandler>(targetButton.Handler);
				var platformButton = Assert.IsAssignableFrom<UIButton>(handler.PlatformView);

				await AssertEventually(() =>
					platformButton.Window is not null &&
					platformButton.Bounds.Width > 0 &&
					platformButton.Bounds.Height > 0);
				Assert.Equal(initialText, platformButton.TitleLabel.AttributedText?.Value);

				platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(() => clickObserved && targetButton.Text == updatedText);
				Assert.True(
					platformButton.TitleLabel.AttributedText?.Value == updatedText,
					"The native attributed button title should update after the click.");
			});
		}
	}
}
#endif
