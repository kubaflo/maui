using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

#if MACCATALYST
[Category("Issue30163")]
[Category(TestCategory.DatePicker)]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue30163 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RuntimeFlowDirectionChangeUpdatesNativeDatePicker()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var callbackSequence = -1;
		var requestedDirectionLabel = new Label
		{
			Text = "Requested FlowDirection: LeftToRight"
		};
		var datePicker = new DatePicker
		{
			Date = new DateTime(2025, 6, 15),
			FlowDirection = FlowDirection.LeftToRight,
			HorizontalOptions = LayoutOptions.Fill
		};
		var toggleButton = new Button
		{
			Text = "ToggleFlowDirection"
		};
		toggleButton.Clicked += (_, _) =>
		{
			datePicker.FlowDirection = FlowDirection.RightToLeft;
			requestedDirectionLabel.Text = "Requested FlowDirection: RightToLeft";
			callbackSequence = 1;
		};

		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "DatePicker FlowDirection on Mac Catalyst",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold
				},
				requestedDirectionLabel,
				datePicker,
				toggleButton,
				new Label { Text = "Observed native direction: LeftToRight" }
			}
		};
		var page = new ContentPage
		{
			Title = "DatePicker FlowDirection",
			Content = layout
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.Equal(new DateTime(2025, 6, 15), datePicker.Date);
			Assert.Equal(FlowDirection.LeftToRight, datePicker.FlowDirection);
			Assert.Equal(LayoutOptions.Fill, datePicker.HorizontalOptions);

			var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			Assert.Same(datePicker, datePickerHandler.VirtualView);
			var nativeDatePicker = Assert.IsType<UIDatePicker>(datePickerHandler.PlatformView);
			Assert.Equal(
				UIUserInterfaceLayoutDirection.LeftToRight,
				nativeDatePicker.EffectiveUserInterfaceLayoutDirection);

			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
			await InvokeOnMainThreadAsync(
				() => nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside));
			await AssertEventually(
				() => callbackSequence == 1,
				message: "ToggleFlowDirection Button Clicked callback did not complete.");
			Assert.Equal(FlowDirection.RightToLeft, datePicker.FlowDirection);

			var observedDirection = (UIUserInterfaceLayoutDirection)(-1);
			await AssertEventually(
				() =>
				{
					observedDirection = nativeDatePicker.EffectiveUserInterfaceLayoutDirection;
					return observedDirection == UIUserInterfaceLayoutDirection.RightToLeft;
				},
				message: "DatePicker native direction after RTL toggle was LeftToRight; expected RightToLeft.");
			Assert.Equal(UIUserInterfaceLayoutDirection.RightToLeft, observedDirection);
		});
	}
}
#endif

