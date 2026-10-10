using System.Threading.Tasks;
using Android.Widget;
using AndroidX.Core.Widget;
using Google.Android.Material.TextField;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

#if ANDROID
[Category("Issue36729")]
public class Issue36729 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DefaultMaterial3DatePickerHasCalendarEndAffordance()
	{
		EnsureHandlerCreated(builder =>
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Microsoft.Maui.Controls.DatePicker, DatePickerHandler2>();
			}));

		var datePicker = new Microsoft.Maui.Controls.DatePicker();
		var label = new Label
		{
			Text = "Date Picker:",
			FontSize = 24
		};
		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				label,
				datePicker
			}
		};
		var page = new ContentPage
		{
			Content = layout
		};

		var attachmentCallbackInvoked = false;
		var handlerType = "<not attached>";
		var platformType = "<not attached>";
		var relativeDrawableCount = -1;
		var hasRelativeEndDrawable = false;
		var hasTextInputLayout = false;
		var endIconMode = int.MinValue;

		await CreateHandlerAndAddToWindow(page, () =>
		{
			attachmentCallbackInvoked = true;

			Assert.NotNull(datePicker.Handler);
			handlerType = datePicker.Handler.GetType().Name;
			Assert.Equal("DatePickerHandler2", handlerType);

			Assert.NotNull(datePicker.Handler.PlatformView);
			var textView = Assert.IsAssignableFrom<TextView>(datePicker.Handler.PlatformView);
			platformType = textView.GetType().FullName ?? textView.GetType().Name;

#pragma warning disable CS0618 // TextViewCompat preserves relative drawable ordering on all supported Android versions.
			var relativeDrawables = TextViewCompat.GetCompoundDrawablesRelative(textView);
#pragma warning restore CS0618
			relativeDrawableCount = relativeDrawables?.Length ?? -1;
			hasRelativeEndDrawable =
				relativeDrawables is not null &&
				relativeDrawables.Length > 2 &&
				relativeDrawables[2] is not null;

			var parent = textView.Parent;
			while (parent is global::Android.Views.View parentView)
			{
				if (parentView is TextInputLayout textInputLayout)
				{
					hasTextInputLayout = true;
					endIconMode = textInputLayout.EndIconMode;
					break;
				}

				parent = parentView.Parent;
			}
		});

		Assert.True(attachmentCallbackInvoked);
		Assert.True(
			hasRelativeEndDrawable ||
			(hasTextInputLayout && endIconMode != TextInputLayout.EndIconNone),
			$"Material 3 DatePicker calendar affordance missing: handler={handlerType}, platform={platformType}, relativeDrawableCount={relativeDrawableCount}, relativeEndDrawable={hasRelativeEndDrawable}, textInputLayout={hasTextInputLayout}, endIconMode={endIconMode}.");
	}
}
#endif

