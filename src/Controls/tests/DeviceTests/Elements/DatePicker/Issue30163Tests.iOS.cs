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

[Category(TestCategory.DatePicker)]
[Category("Issue30163")]
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
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var datePicker = new DatePicker
		{
			FlowDirection = FlowDirection.LeftToRight,
			Format = "MM/dd/yyyy",
			Date = new DateTime(2002, 5, 14)
		};
		var initialDirectionLabel = new Label { Text = "Initial direction: LeftToRight" };
		var statusLabel = new Label { Text = "Target direction: RightToLeft" };
		var toggleButton = new Button { Text = "Toggle FlowDirection" };
		var callbackInvoked = false;

		toggleButton.Clicked += (_, _) =>
		{
			callbackInvoked = true;
			datePicker.FlowDirection = FlowDirection.RightToLeft;
		};

		var layout = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center
		};
		layout.Add(new Label { Text = "DatePicker before and after changing FlowDirection to RightToLeft:" });
		layout.Add(datePicker);
		layout.Add(initialDirectionLabel);
		layout.Add(toggleButton);
		layout.Add(statusLabel);

		var page = new ContentPage
		{
			Title = "DatePicker FlowDirection",
			Content = layout
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var nativeDatePicker = Assert.IsType<UIDatePicker>(datePickerHandler.PlatformView);

			Assert.NotNull(nativeDatePicker.Window);
			Assert.Equal(FlowDirection.LeftToRight, datePicker.FlowDirection);
			Assert.Equal(
				UIUserInterfaceLayoutDirection.LeftToRight,
				nativeDatePicker.EffectiveUserInterfaceLayoutDirection);

			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			await AssertEventually(
				() => callbackInvoked,
				message: "Issue30163: native button did not invoke the FlowDirection callback");
			Assert.Equal(FlowDirection.RightToLeft, datePicker.FlowDirection);
			Assert.Same(nativeDatePicker, datePickerHandler.PlatformView);
			Assert.NotNull(nativeDatePicker.Window);

			await AssertEventually(
				() => nativeDatePicker.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft,
				message: "Issue30163: native DatePicker direction did not become RightToLeft");
		});
	}
}
#endif

