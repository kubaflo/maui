#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category("Issue30163")]
public class Issue30163 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DatePickerFlowDirectionUpdatesAfterButtonClick()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var datePicker = new DatePicker
		{
			FlowDirection = FlowDirection.LeftToRight
		};
		var expectedDirectionLabel = new Label
		{
			Text = "Expected direction: LeftToRight"
		};
		var toggleButton = new Button
		{
			Text = "Toggle FlowDirection"
		};
		var clickCallbackMarker = -1;
		toggleButton.Clicked += (_, _) =>
		{
			expectedDirectionLabel.Text = "Expected direction: RightToLeft";
			datePicker.FlowDirection = FlowDirection.RightToLeft;
			clickCallbackMarker = 1;
		};

		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "DatePicker FlowDirection on Mac Catalyst", FontSize = 20 },
					datePicker,
					expectedDirectionLabel,
					new Label { Text = "Native direction: waiting" },
					toggleButton
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.NotNull(datePicker.Handler);
			var platformDatePicker = Assert.IsType<UIDatePicker>(datePicker.Handler.PlatformView);
			var initialDirection = platformDatePicker.EffectiveUserInterfaceLayoutDirection;
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, initialDirection);

			Assert.NotNull(toggleButton.Handler);
			var platformButton = Assert.IsAssignableFrom<UIButton>(toggleButton.Handler.PlatformView);
			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			await AssertEventually(
				() => clickCallbackMarker == 1,
				message: "The native button did not invoke the FlowDirection toggle callback.");
			Assert.Equal(FlowDirection.RightToLeft, datePicker.FlowDirection);

			var observedDirection = initialDirection;
			var directionUpdated = await Wait(
				() =>
				{
					observedDirection = platformDatePicker.EffectiveUserInterfaceLayoutDirection;
					return observedDirection == UIUserInterfaceLayoutDirection.RightToLeft;
				});

			Assert.True(directionUpdated, $"DatePicker native direction did not follow FlowDirection after the toggle callback. Initial: {initialDirection}; observed: {observedDirection}; expected: {UIUserInterfaceLayoutDirection.RightToLeft}.");
			Assert.Equal(UIUserInterfaceLayoutDirection.RightToLeft, observedDirection);
		});
	}
}
#endif

