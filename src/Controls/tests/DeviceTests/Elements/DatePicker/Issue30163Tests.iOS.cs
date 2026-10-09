#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

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
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var testDate = new DateTime(2002, 5, 14);
		var topDatePicker = new DatePicker
		{
			Date = testDate,
			FlowDirection = FlowDirection.RightToLeft,
			Format = "MM/dd/yyyy",
			WidthRequest = 300,
			HorizontalOptions = LayoutOptions.Center
		};
		var bottomDatePicker = new DatePicker
		{
			Date = testDate,
			FlowDirection = FlowDirection.LeftToRight,
			Format = "MM/dd/yyyy",
			WidthRequest = 300,
			HorizontalOptions = LayoutOptions.Center
		};
		var directionLabel = new Label
		{
			Text = "Top: RightToLeft; Bottom: LeftToRight",
			HorizontalOptions = LayoutOptions.Center
		};
		var toggleButton = new Button
		{
			Text = "Toggle FlowDirection",
			HorizontalOptions = LayoutOptions.Center
		};
		var layout = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "DatePicker FlowDirection on Mac Catalyst",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center
				},
				topDatePicker,
				bottomDatePicker,
				directionLabel,
				toggleButton
			}
		};
		var page = new ContentPage { Content = layout };
		var clickCount = 0;
		UIUserInterfaceLayoutDirection? topNativeDirection = null;
		UIUserInterfaceLayoutDirection? bottomNativeDirection = null;
		var postClick = new TaskCompletionSource();

		toggleButton.Clicked += (_, _) =>
		{
			clickCount++;
			topDatePicker.FlowDirection = FlowDirection.LeftToRight;
			bottomDatePicker.FlowDirection = FlowDirection.RightToLeft;
			directionLabel.Text = "Top: LeftToRight; Bottom: RightToLeft";

			page.Dispatcher.Dispatch(() =>
			{
				var topHandler = Assert.IsType<DatePickerHandler>(topDatePicker.Handler);
				var bottomHandler = Assert.IsType<DatePickerHandler>(bottomDatePicker.Handler);
				var topPlatformView = Assert.IsAssignableFrom<UIDatePicker>(topHandler.PlatformView);
				var bottomPlatformView = Assert.IsAssignableFrom<UIDatePicker>(bottomHandler.PlatformView);

				topNativeDirection = topPlatformView.EffectiveUserInterfaceLayoutDirection;
				bottomNativeDirection = bottomPlatformView.EffectiveUserInterfaceLayoutDirection;
				postClick.TrySetResult();
			});
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			var topHandler = Assert.IsType<DatePickerHandler>(topDatePicker.Handler);
			var bottomHandler = Assert.IsType<DatePickerHandler>(bottomDatePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var topPlatformView = Assert.IsAssignableFrom<UIDatePicker>(topHandler.PlatformView);
			var bottomPlatformView = Assert.IsAssignableFrom<UIDatePicker>(bottomHandler.PlatformView);
			var platformButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);

			Assert.Equal(testDate, topDatePicker.Date);
			Assert.Equal(testDate, bottomDatePicker.Date);
			Assert.Equal("MM/dd/yyyy", topDatePicker.Format);
			Assert.Equal("MM/dd/yyyy", bottomDatePicker.Format);
			Assert.Equal(FlowDirection.RightToLeft, topDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.LeftToRight, bottomDatePicker.FlowDirection);
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, bottomPlatformView.EffectiveUserInterfaceLayoutDirection);

			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			await postClick.Task.WaitAsync(TimeSpan.FromSeconds(2));

			Assert.Equal(1, clickCount);
			Assert.True(topNativeDirection.HasValue);
			Assert.True(bottomNativeDirection.HasValue);
			Assert.Equal(FlowDirection.LeftToRight, topDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.RightToLeft, bottomDatePicker.FlowDirection);
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, topNativeDirection.Value);
			Assert.True(
				bottomNativeDirection.Value == UIUserInterfaceLayoutDirection.RightToLeft,
				$"Issue30163 bottom DatePicker native flow direction did not update to RightToLeft. Expected: {UIUserInterfaceLayoutDirection.RightToLeft}; Observed: {bottomNativeDirection.Value}.");
		});
	}
}
#endif

