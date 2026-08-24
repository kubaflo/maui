using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

#if IOS && !MACCATALYST
[Category("Issue36933")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36933 : ControlsHandlerTestBase
{
	[Fact]
	public async Task ClearingPickerBackgroundRemovesAppliedColor()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(Microsoft.Maui.Controls.Window), typeof(WindowHandlerStub));
				handlers.AddHandler(typeof(ContentPage), typeof(PageHandler));
				handlers.AddHandler(typeof(VerticalStackLayout), typeof(LayoutHandler));
				handlers.AddHandler(typeof(Label), typeof(LabelHandler));
				handlers.AddHandler(typeof(Button), typeof(ButtonHandler));
				handlers.AddHandler(typeof(DatePicker), typeof(DatePickerHandler));
				handlers.AddHandler(typeof(TimePicker), typeof(TimePickerHandler));
			});
		});

		var datePicker = new DatePicker();
		var timePicker = new TimePicker();
		var toggleButton = new Button { Text = "Apply picker backgrounds" };
		var checkButton = new Button
		{
			Text = "Check cleared backgrounds",
			IsEnabled = false,
		};
		var phase = -1;

		toggleButton.Clicked += (_, _) =>
		{
			if (phase < 1)
			{
				datePicker.Background = new SolidColorBrush(Colors.Red);
				timePicker.Background = new SolidColorBrush(Colors.Red);
				toggleButton.Text = "Clear picker backgrounds";
				phase = 1;
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			toggleButton.Text = "Apply picker backgrounds";
			checkButton.IsEnabled = true;
			phase = 2;
		};

		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "DatePicker and TimePicker background clearing",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold,
				},
				new Label { Text = "Apply a red background, then clear it and inspect the native picker backgrounds." },
				datePicker,
				timePicker,
				toggleButton,
				checkButton,
				new Label { Text = "REFERENCE: Red is the applied color." },
				new Label
				{
					Text = "Inspect the picker backgrounds after clearing.",
					FontAttributes = FontAttributes.Bold,
				},
			},
		};
		var page = new ContentPage
		{
			Title = "Picker background clearing",
			Content = layout,
		};

		await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
		{
			Assert.NotNull(datePicker.Handler);
			Assert.NotNull(timePicker.Handler);
			Assert.NotNull(toggleButton.Handler);

			var nativeToggleButton = Assert.IsAssignableFrom<UIButton>(toggleButton.Handler.PlatformView);

			Assert.False(HasOpaqueRedNativeBackground(datePicker));
			Assert.False(HasOpaqueRedNativeBackground(timePicker));

			nativeToggleButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			await AssertEventually(
				() => phase == 1,
				message: "The first native button action did not complete.");
			await AssertEventually(
				() => HasOpaqueRedNativeBackground(datePicker) && HasOpaqueRedNativeBackground(timePicker),
				message: "Both native picker backgrounds did not become opaque red.");

			nativeToggleButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			await AssertEventually(
				() => phase == 2,
				message: "The second native button action did not complete.");
			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);

			await AssertEventually(
				() => !HasOpaqueRedNativeBackground(datePicker) &&
					!HasOpaqueRedNativeBackground(timePicker),
				message: "native background remained opaque red after Background was cleared");
		});
	}

	static bool HasOpaqueRedNativeBackground(VisualElement picker)
	{
		Assert.NotNull(picker.Handler);
		var platformView = Assert.IsAssignableFrom<UIView>(picker.Handler.PlatformView);
		return IsOpaqueRed(platformView.BackgroundColor);
	}

	static bool IsOpaqueRed(UIColor color)
	{
		if (color is null)
			return false;

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return red > 0.9 && green < 0.1 && blue < 0.1 && alpha > 0.9;
	}
}
#endif

