using System;
using System.Linq;
using System.Threading.Tasks;
using Android.OS;
using Google.Android.Material.TextField;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AImageView = Android.Widget.ImageView;
using AView = Android.Views.View;
using MotionEvent = Android.Views.MotionEvent;
using MotionEventActions = Android.Views.MotionEventActions;
using ViewStates = Android.Views.ViewStates;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category(TestCategory.DatePicker)]
[Category("Issue36729")]
public class Issue36729 : ControlsHandlerTestBase
{
	const string Material3SwitchName = "Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled";

#if ANDROID
	[Fact]
	public async Task DefaultDatePickerUsesOutlinedMaterial3FieldWithCalendarIcon()
	{
		bool hadOriginalValue = AppContext.TryGetSwitch(Material3SwitchName, out bool originalValue);

		try
		{
			AppContext.SetSwitch(Material3SwitchName, true);
			Assert.True(RuntimeFeature.IsMaterial3Enabled);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddControlsHandlers();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			await VerifyMaterial3NativeOracle();

			int revealCommandObserved = -1;
			int visibleNativeLayoutObserved = -1;
			var affectedDatePicker = new DatePicker
			{
				IsVisible = false
			};
			var revealButton = new Button
			{
				Text = "Show default DatePicker"
			};
			revealButton.Clicked += (_, _) =>
			{
				revealCommandObserved = 1;
				affectedDatePicker.IsVisible = true;
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "Date Picker:", FontSize = 24 },
						revealButton,
						affectedDatePicker,
						new Label { Text = "Expected: outlined Material 3 field with calendar icon", FontSize = 16 },
						new Button { Text = "Check Material 3 styling" },
						new Label { Text = "Ready for inspection", FontSize = 18 }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(affectedDatePicker.Handler);
				var datePickerNativeView = Assert.IsAssignableFrom<AView>(affectedDatePicker.Handler.PlatformView);
				Assert.NotEqual(ViewStates.Visible, datePickerNativeView.Visibility);
				Assert.True(datePickerNativeView.Width == 0 || datePickerNativeView.Height == 0);

				datePickerNativeView.LayoutChange += OnDatePickerLayoutChanged;
				try
				{
					Assert.NotNull(revealButton.Handler);
					var revealButtonNativeView = Assert.IsAssignableFrom<AView>(revealButton.Handler.PlatformView);
					await revealButtonNativeView.WaitForLayoutOrNonZeroSize();

					DispatchTap(revealButtonNativeView);

					await AssertEventually(
						() => revealCommandObserved == 1,
						message: "The native reveal Button tap did not execute its MAUI command.");
					Assert.Equal(1, revealCommandObserved);

					await AssertEventually(
						() => affectedDatePicker.IsVisible && datePickerNativeView.Visibility == ViewStates.Visible,
						message: "The DatePicker did not become natively visible after the reveal command.");

					await AssertEventually(
						() => visibleNativeLayoutObserved == 1,
						message: "The revealed DatePicker did not complete a nonzero native layout.");
					Assert.Equal(1, visibleNativeLayoutObserved);
					Assert.True(datePickerNativeView.Width > 0 && datePickerNativeView.Height > 0);

					var textInputLayout = FindContainingTextInputLayout(datePickerNativeView);
					var endIconView = textInputLayout?
						.GetChildrenOfType<AImageView>()
						.FirstOrDefault(icon =>
							icon.Drawable is not null &&
							icon.Visibility == ViewStates.Visible);
					bool hasEndIconDrawable = endIconView?.Drawable is not null;
					bool isEndIconVisible = endIconView is not null &&
						endIconView.Visibility == ViewStates.Visible &&
						endIconView.Width > 0 &&
						endIconView.Height > 0;
					int boxBackgroundMode = textInputLayout?.BoxBackgroundMode ?? -1;

					Assert.True(
						textInputLayout is not null &&
						boxBackgroundMode == TextInputLayout.BoxBackgroundOutline &&
						textInputLayout.EndIconMode != TextInputLayout.EndIconNone &&
						hasEndIconDrawable &&
						isEndIconVisible,
						$"Material 3 DatePicker native field styling mismatch: " +
						$"nativeType={datePickerNativeView.GetType().FullName}, " +
						$"containerType={textInputLayout?.GetType().FullName ?? "none"}, " +
						$"boxMode={boxBackgroundMode}, " +
						$"endIconDrawable={hasEndIconDrawable}, " +
						$"endIconVisible={isEndIconVisible}, " +
						$"targetSize={datePickerNativeView.Width}x{datePickerNativeView.Height}, " +
						$"containerSize={textInputLayout?.Width ?? 0}x{textInputLayout?.Height ?? 0}.");
				}
				finally
				{
					datePickerNativeView.LayoutChange -= OnDatePickerLayoutChanged;
				}

				void OnDatePickerLayoutChanged(object sender, AView.LayoutChangeEventArgs e)
				{
					if (datePickerNativeView.Visibility == ViewStates.Visible &&
						datePickerNativeView.Width > 0 &&
						datePickerNativeView.Height > 0)
					{
						visibleNativeLayoutObserved = 1;
					}
				}
			});
		}
		finally
		{
			AppContext.SetSwitch(Material3SwitchName, hadOriginalValue && originalValue);
		}
	}
#endif

	async Task VerifyMaterial3NativeOracle()
	{
		var entry = new Entry();

		await CreateHandlerAndAddToWindow(entry, async () =>
		{
			Assert.NotNull(entry.Handler);
			var entryNativeView = Assert.IsAssignableFrom<AView>(entry.Handler.PlatformView);
			await entryNativeView.WaitForLayoutOrNonZeroSize();

			var textInputLayout = Assert.IsAssignableFrom<TextInputLayout>(entryNativeView);
			Assert.True(textInputLayout.Width > 0 && textInputLayout.Height > 0);
			Assert.Equal(TextInputLayout.BoxBackgroundOutline, textInputLayout.BoxBackgroundMode);
		});
	}

	static TextInputLayout FindContainingTextInputLayout(AView nativeView)
	{
		if (nativeView is TextInputLayout textInputLayout)
			return textInputLayout;

		var parent = nativeView.Parent;
		while (parent is AView parentView)
		{
			if (parentView is TextInputLayout parentTextInputLayout &&
				ReferenceEquals(parentTextInputLayout.EditText, nativeView))
			{
				return parentTextInputLayout;
			}

			parent = parentView.Parent;
		}

		return null;
	}

	static void DispatchTap(AView nativeView)
	{
		long downTime = SystemClock.UptimeMillis();
		float x = nativeView.Width / 2f;
		float y = nativeView.Height / 2f;
		var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down, x, y, 0);
		var up = MotionEvent.Obtain(downTime, downTime + 16, MotionEventActions.Up, x, y, 0);

		try
		{
			Assert.True(nativeView.DispatchTouchEvent(down));
			Assert.True(nativeView.DispatchTouchEvent(up));
		}
		finally
		{
			down.Recycle();
			up.Recycle();
		}
	}
}

