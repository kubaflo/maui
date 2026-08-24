#if MACCATALYST
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

[Category(TestCategory.DatePicker)]
[Category("Issue30163")]
public class Issue30163 : ControlsHandlerTestBase
{
	[Fact]
	public async Task FlowDirectionUpdatesAfterButtonClick()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var testDate = new DateTime(2002, 5, 14);
		var firstDatePicker = new DatePicker
		{
			Date = testDate,
			FlowDirection = FlowDirection.RightToLeft,
			Format = "MM/dd/yyyy",
			WidthRequest = 300
		};
		var secondDatePicker = new DatePicker
		{
			Date = testDate,
			FlowDirection = FlowDirection.LeftToRight,
			Format = "MM/dd/yyyy",
			WidthRequest = 300
		};
		var toggleButton = new Button { Text = "Toggle FlowDirection" };
		var statusLabel = new Label { Text = "Ready" };
		var clickCount = -1;

		toggleButton.Clicked += (_, _) =>
		{
			clickCount = 1;
			firstDatePicker.FlowDirection = FlowDirection.LeftToRight;
			secondDatePicker.FlowDirection = FlowDirection.RightToLeft;
			statusLabel.Text = "Toggled";
		};

		var layout = new VerticalStackLayout
		{
			Padding = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				firstDatePicker,
				secondDatePicker,
				toggleButton,
				statusLabel
			}
		};
		var page = new ContentPage { Content = layout };
		var window = new Window(page);

		await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
		{
			Assert.Equal(FlowDirection.RightToLeft, firstDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.LeftToRight, secondDatePicker.FlowDirection);
			Assert.Equal(testDate, firstDatePicker.Date);
			Assert.Equal(testDate, secondDatePicker.Date);
			Assert.Equal("MM/dd/yyyy", firstDatePicker.Format);
			Assert.Equal("MM/dd/yyyy", secondDatePicker.Format);
			Assert.Equal(300, firstDatePicker.WidthRequest);
			Assert.Equal(300, secondDatePicker.WidthRequest);
			Assert.NotSame(firstDatePicker, secondDatePicker);

			var firstHandler = Assert.IsType<DatePickerHandler>(firstDatePicker.Handler);
			var secondHandler = Assert.IsType<DatePickerHandler>(secondDatePicker.Handler);
			var firstNativePicker = Assert.IsType<UIDatePicker>(firstHandler.PlatformView);
			var secondNativePicker = Assert.IsType<UIDatePicker>(secondHandler.PlatformView);
			Assert.NotNull(firstNativePicker.Window);
			Assert.NotNull(secondNativePicker.Window);
			Assert.NotSame(firstNativePicker, secondNativePicker);

			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			await AssertEventually(
				() => clickCount == 1 &&
					firstDatePicker.FlowDirection == FlowDirection.LeftToRight &&
					secondDatePicker.FlowDirection == FlowDirection.RightToLeft &&
					statusLabel.Text == "Toggled",
				message: "The native button action did not complete the managed FlowDirection transition.");

			var firstNativeDirection = firstNativePicker.SemanticContentAttribute;
			var secondNativeDirection = secondNativePicker.SemanticContentAttribute;

			await AssertEventually(
				() =>
				{
					firstNativeDirection = firstNativePicker.SemanticContentAttribute;
					secondNativeDirection = secondNativePicker.SemanticContentAttribute;
					return firstNativeDirection == UISemanticContentAttribute.ForceLeftToRight &&
						secondNativeDirection == UISemanticContentAttribute.ForceRightToLeft;
				},
				message: $"DatePicker native flow direction did not update after toggle: expected ForceLeftToRight/ForceRightToLeft, measured {firstNativeDirection}/{secondNativeDirection}.");
		});
	}
}
#endif

