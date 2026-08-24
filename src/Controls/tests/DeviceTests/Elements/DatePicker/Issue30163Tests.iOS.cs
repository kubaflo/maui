#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

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
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var requestedDirectionLabel = new Label
		{
			Text = "Requested direction: LeftToRight",
			HorizontalOptions = LayoutOptions.Center
		};
		var datePicker = new DatePicker
		{
			FlowDirection = FlowDirection.LeftToRight,
			HorizontalOptions = LayoutOptions.Center
		};
		var toggleButton = new Button { Text = "Toggle FlowDirection" };
		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center
		};
		layout.Add(new Label
		{
			Text = "Mac Catalyst DatePicker FlowDirection",
			FontSize = 20,
			HorizontalOptions = LayoutOptions.Center
		});
		layout.Add(requestedDirectionLabel);
		layout.Add(datePicker);
		layout.Add(toggleButton);

		var page = new ContentPage
		{
			Title = "DatePicker FlowDirection",
			Content = layout
		};

		var clickCount = 0;
		var directionChangedCount = 0;
		toggleButton.Clicked += (_, _) =>
		{
			clickCount++;
			datePicker.FlowDirection = FlowDirection.RightToLeft;
			requestedDirectionLabel.Text = "Requested direction: RightToLeft";
		};
		datePicker.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(VisualElement.FlowDirection))
				directionChangedCount++;
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var platformDatePicker = Assert.IsType<UIDatePicker>(datePickerHandler.PlatformView);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var platformButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);

			Assert.Equal(FlowDirection.LeftToRight, datePicker.FlowDirection);
			Assert.Equal(
				UIUserInterfaceLayoutDirection.LeftToRight,
				platformDatePicker.EffectiveUserInterfaceLayoutDirection);

			var observedDirection = (UIUserInterfaceLayoutDirection)(-1);
			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			var triggerCompleted = await AssertHelpers.Wait(
				() => clickCount == 1 && directionChangedCount == 1);
			Assert.True(triggerCompleted, "The attached button did not change DatePicker.FlowDirection.");
			Assert.Equal(FlowDirection.RightToLeft, datePicker.FlowDirection);

			var directionUpdated = await AssertHelpers.Wait(() =>
			{
				observedDirection = platformDatePicker.EffectiveUserInterfaceLayoutDirection;
				return observedDirection == UIUserInterfaceLayoutDirection.RightToLeft;
			});

			Assert.True(
				directionUpdated,
				$"DatePicker native FlowDirection expected RightToLeft after toggle but was {observedDirection}");
		});
	}
}
#endif

