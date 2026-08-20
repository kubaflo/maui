using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

#if MACCATALYST
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
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
			});
		});

		var date = new DateTime(2002, 5, 14);
		var firstDirectionLabel = new Label
		{
			Text = "First expected direction: RightToLeft",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var firstDatePicker = new DatePicker
		{
			Date = date,
			Format = "MM/dd/yyyy",
			FlowDirection = FlowDirection.RightToLeft,
			WidthRequest = 300
		};
		var secondDirectionLabel = new Label
		{
			Text = "Second expected direction: LeftToRight",
			HorizontalTextAlignment = TextAlignment.Center
		};
		var secondDatePicker = new DatePicker
		{
			Date = date,
			Format = "MM/dd/yyyy",
			FlowDirection = FlowDirection.LeftToRight,
			WidthRequest = 300
		};
		var toggleButton = new Button { Text = "Toggle FlowDirection" };
		var resultLabel = new Label
		{
			Text = "NO BUG:",
			FontAttributes = FontAttributes.Bold,
			HorizontalTextAlignment = TextAlignment.Center
		};
		var layout = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 12,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "The first picker starts RightToLeft; the second starts LeftToRight. Toggle once to swap them.",
					HorizontalTextAlignment = TextAlignment.Center
				},
				firstDirectionLabel,
				firstDatePicker,
				secondDirectionLabel,
				secondDatePicker,
				toggleButton,
				resultLabel
			}
		};
		var page = new ContentPage { Content = layout };

		var toggleRequested = false;
		var firstDirectionChanged = false;
		var secondDirectionChanged = false;
		firstDatePicker.PropertyChanged += (_, args) =>
		{
			if (toggleRequested && args.PropertyName == nameof(DatePicker.FlowDirection))
				firstDirectionChanged = true;
		};
		secondDatePicker.PropertyChanged += (_, args) =>
		{
			if (toggleRequested && args.PropertyName == nameof(DatePicker.FlowDirection))
				secondDirectionChanged = true;
		};
		toggleButton.Clicked += (_, _) =>
		{
			firstDatePicker.FlowDirection = FlowDirection.LeftToRight;
			secondDatePicker.FlowDirection = FlowDirection.RightToLeft;
			firstDirectionLabel.Text = "First expected direction: LeftToRight";
			secondDirectionLabel.Text = "Second expected direction: RightToLeft";
			resultLabel.Text = "Directions toggled";
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			Assert.Equal(date, firstDatePicker.Date);
			Assert.Equal(date, secondDatePicker.Date);
			Assert.Equal("MM/dd/yyyy", firstDatePicker.Format);
			Assert.Equal("MM/dd/yyyy", secondDatePicker.Format);
			Assert.Equal(FlowDirection.RightToLeft, firstDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.LeftToRight, secondDatePicker.FlowDirection);

			var firstHandler = Assert.IsType<DatePickerHandler>(firstDatePicker.Handler);
			var secondHandler = Assert.IsType<DatePickerHandler>(secondDatePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var firstNativePicker = Assert.IsType<UIDatePicker>(firstHandler.PlatformView);
			var secondNativePicker = Assert.IsType<UIDatePicker>(secondHandler.PlatformView);

			Assert.NotNull(firstNativePicker.Window);
			Assert.NotNull(secondNativePicker.Window);
			Assert.True(firstNativePicker.Frame.Width > 0 && firstNativePicker.Frame.Height > 0);
			Assert.True(secondNativePicker.Frame.Width > 0 && secondNativePicker.Frame.Height > 0);
			Assert.Equal(UIUserInterfaceLayoutDirection.LeftToRight, secondNativePicker.EffectiveUserInterfaceLayoutDirection);

			toggleRequested = true;
			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.True(firstDirectionChanged, "The first DatePicker did not report its post-trigger FlowDirection change.");
			Assert.True(secondDirectionChanged, "The second DatePicker did not report its post-trigger FlowDirection change.");
			Assert.Equal(FlowDirection.LeftToRight, firstDatePicker.FlowDirection);
			Assert.Equal(FlowDirection.RightToLeft, secondDatePicker.FlowDirection);
			Assert.Equal("Directions toggled", resultLabel.Text);

			var firstDirectionUpdated = await Wait(
				() => firstNativePicker.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.LeftToRight);
			var secondDirectionUpdated = await Wait(
				() => secondNativePicker.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft);

			Assert.True(firstDirectionUpdated,
				$"Issue30163 clean DatePicker native direction was {firstNativePicker.EffectiveUserInterfaceLayoutDirection}; expected LeftToRight.");
			Assert.True(secondDirectionUpdated,
				$"Issue30163 Mac Catalyst DatePicker native direction after toggle was {secondNativePicker.EffectiveUserInterfaceLayoutDirection}; expected RightToLeft.");
		});
	}
}
#endif
