#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category(TestCategory.DatePicker)]
[Category("Issue30163")]
public class Issue30163 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RuntimeFlowDirectionChangeUpdatesNativeDatePicker()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var date = new DateTime(2002, 5, 14);
		var initialRightToLeftDatePicker = new DatePicker
		{
			Date = date,
			FlowDirection = FlowDirection.RightToLeft,
			Format = "MM/dd/yyyy",
			WidthRequest = 300
		};
		var initialLeftToRightDatePicker = new DatePicker
		{
			Date = date,
			FlowDirection = FlowDirection.LeftToRight,
			Format = "MM/dd/yyyy",
			WidthRequest = 300
		};
		var toggleButton = new Button
		{
			Text = "Toggle FlowDirection"
		};
		var descriptionLabel = new Label
		{
			Text = "Toggle the flow direction"
		};
		var layout = new VerticalStackLayout
		{
			Padding = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				initialRightToLeftDatePicker,
				initialLeftToRightDatePicker,
				toggleButton,
				descriptionLabel
			}
		};
		var page = new ContentPage
		{
			Content = layout
		};

		var callbackFired = false;
		toggleButton.Clicked += (_, _) =>
		{
			callbackFired = true;
			initialRightToLeftDatePicker.FlowDirection = FlowDirection.LeftToRight;
			initialLeftToRightDatePicker.FlowDirection = FlowDirection.RightToLeft;
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			var initialRightToLeftHandler = Assert.IsAssignableFrom<DatePickerHandler>(initialRightToLeftDatePicker.Handler);
			var initialLeftToRightHandler = Assert.IsAssignableFrom<DatePickerHandler>(initialLeftToRightDatePicker.Handler);
			var buttonHandler = Assert.IsAssignableFrom<ButtonHandler>(toggleButton.Handler);
			Assert.IsAssignableFrom<UIDatePicker>(initialRightToLeftHandler.PlatformView);
			var initialLeftToRightPlatformView = Assert.IsAssignableFrom<UIDatePicker>(initialLeftToRightHandler.PlatformView);
			var platformButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);

			Assert.Equal(date, initialRightToLeftDatePicker.Date);
			Assert.Equal(date, initialLeftToRightDatePicker.Date);
			Assert.Equal("MM/dd/yyyy", initialRightToLeftDatePicker.Format);
			Assert.Equal("MM/dd/yyyy", initialLeftToRightDatePicker.Format);
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, initialLeftToRightPlatformView.EffectiveUserInterfaceLayoutDirection);

			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.True(callbackFired, "The attached Button did not invoke its Clicked callback.");
			Assert.Equal(FlowDirection.LeftToRight, initialRightToLeftDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.RightToLeft, initialLeftToRightDatePicker.FlowDirection);

			var expectedDirection = UIUserInterfaceLayoutDirection.RightToLeft;
			await AssertEventually(
				() => initialLeftToRightPlatformView.EffectiveUserInterfaceLayoutDirection == expectedDirection,
				message: $"Mac Catalyst DatePicker native flow direction did not update after toggle: measured {initialLeftToRightPlatformView.EffectiveUserInterfaceLayoutDirection}; expected {expectedDirection}.");
			Assert.Equal(expectedDirection, initialLeftToRightPlatformView.EffectiveUserInterfaceLayoutDirection);
		});
	}
}
#endif

