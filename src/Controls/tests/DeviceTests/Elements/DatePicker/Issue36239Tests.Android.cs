using System;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AOrientation = Android.Content.Res.Orientation;
using AWindow = Microsoft.Maui.Controls.Window;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.DatePicker)]
public class Issue36239 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DatePickerDialogInheritsSecureFlagFromHostWindow()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<AWindow, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var activity = MauiContext.Context.GetActivity();
		var hostWindow = activity.Window;
		var originalFlags = hostWindow.Attributes.Flags;
		var originalOrientation = activity.RequestedOrientation;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				activity.RequestedOrientation = ScreenOrientation.Portrait;
				hostWindow.AddFlags(WindowManagerFlags.Secure);
			});

			await AssertEventually(
				() => activity.Resources.Configuration.Orientation == AOrientation.Portrait,
				message: "The Android device did not enter the required portrait orientation.");
			Assert.True((hostWindow.Attributes.Flags & WindowManagerFlags.Secure) != 0);

			var datePicker = new DatePicker
			{
				Date = new DateTime(2026, 8, 18),
				Format = "yyyy-MM-dd"
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 20,
					Children =
					{
						new Label
						{
							Text = "Protected date dialog",
							FontAttributes = FontAttributes.Bold,
							FontSize = 24
						},
						new Label
						{
							Text = "The host window uses its screen security policy. Open the platform-default date dialog.",
							FontSize = 16
						},
						datePicker,
						new Label
						{
							Text = "NO BUG:",
							FontAttributes = FontAttributes.Bold,
							FontSize = 18
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
				var platformDatePicker = handler.PlatformView;
				Assert.Equal("2026-08-18", platformDatePicker.Text);
				await AssertEventually(
					() => platformDatePicker.IsAttachedToWindow && platformDatePicker.Width > 0 && platformDatePicker.Height > 0,
					message: "The native DatePicker was not attached and laid out.");

				var openedCount = 0;
				var observedDialogFlags = (WindowManagerFlags)(-1);
				datePicker.Opened += (_, _) =>
				{
					openedCount++;
					var dialogWindow = handler.DatePickerDialog?.Window;
					if (dialogWindow is not null)
						observedDialogFlags = dialogWindow.Attributes.Flags;
				};

				var eventTime = SystemClock.UptimeMillis();
				using var downEvent = MotionEvent.Obtain(
					eventTime,
					eventTime,
					MotionEventActions.Down,
					platformDatePicker.Width / 2f,
					platformDatePicker.Height / 2f,
					0);
				platformDatePicker.DispatchTouchEvent(downEvent);

				using var upEvent = MotionEvent.Obtain(
					eventTime,
					eventTime + 16,
					MotionEventActions.Up,
					platformDatePicker.Width / 2f,
					platformDatePicker.Height / 2f,
					0);
				platformDatePicker.DispatchTouchEvent(upEvent);

				await AssertEventually(
					() => openedCount > 0 &&
						datePicker.IsOpen &&
						handler.DatePickerDialog?.IsShowing == true &&
						observedDialogFlags != (WindowManagerFlags)(-1),
					message: "The touch input did not open the production DatePickerDialog and capture its flags.");

				Assert.True(
					(observedDialogFlags & WindowManagerFlags.Secure) != 0,
					$"Framework-created DatePickerDialog security flags: expected {WindowManagerFlags.Secure}; observed dialog flags {observedDialogFlags}; host flags {hostWindow.Attributes.Flags}.");
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				if ((originalFlags & WindowManagerFlags.Secure) == 0)
					hostWindow.ClearFlags(WindowManagerFlags.Secure);

				activity.RequestedOrientation = originalOrientation;
			});
		}
	}
}
