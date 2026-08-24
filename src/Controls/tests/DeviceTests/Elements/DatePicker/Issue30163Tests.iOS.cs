#if MACCATALYST
using System;
using System.ComponentModel;
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

		var clickObserved = false;
		var changedFlowDirection = (FlowDirection)(-1);
		var instructionLabel = new Label
		{
			Text = "The date picker starts left-to-right. Toggle it to right-to-left."
		};
		var datePicker = new DatePicker();
		var toggleButton = new Button
		{
			Text = "Toggle FlowDirection"
		};
		var actionStatusLabel = new Label
		{
			Text = "Waiting for toggle"
		};
		var expectationLabel = new Label
		{
			Text = "The native flow direction should match the DatePicker flow direction."
		};

		datePicker.PropertyChanged += OnDatePickerPropertyChanged;
		toggleButton.Clicked += OnToggleFlowDirectionClicked;

		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					instructionLabel,
					datePicker,
					toggleButton,
					actionStatusLabel,
					expectationLabel
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var platformDatePicker = Assert.IsType<UIDatePicker>(datePickerHandler.PlatformView);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var platformButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);

			Assert.Equal(FlowDirection.MatchParent, datePicker.FlowDirection);
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, platformDatePicker.EffectiveUserInterfaceLayoutDirection);

			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.True(clickObserved, "Issue30163: the native button click did not reach the Clicked callback.");
			Assert.Equal(FlowDirection.RightToLeft, changedFlowDirection);
			Assert.Equal(FlowDirection.RightToLeft, datePicker.FlowDirection);

			var nativeFlowDirectionUpdated = await Wait(
				() => platformDatePicker.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft);
			var finalNativeFlowDirection = platformDatePicker.EffectiveUserInterfaceLayoutDirection;

			Assert.True(
				nativeFlowDirectionUpdated,
				$"Issue30163: DatePicker native flow direction remained {finalNativeFlowDirection}; expected RightToLeft.");
		});

		void OnDatePickerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.FlowDirectionProperty.PropertyName)
				changedFlowDirection = datePicker.FlowDirection;
		}

		void OnToggleFlowDirectionClicked(object sender, EventArgs e)
		{
			clickObserved = true;
			datePicker.FlowDirection = FlowDirection.RightToLeft;
			actionStatusLabel.Text = "RTL requested";
		}
	}
}
#endif

