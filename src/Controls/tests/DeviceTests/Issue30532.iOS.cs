#if MACCATALYST
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CoreFoundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30532")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue30532 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CharacterSpacingExpandsNativeTimePickerWidth()
		{
			const double characterSpacing = 4;
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<TimePicker, TimePickerHandler>();
				});
			});

			var heading = new Label
			{
				Text = "TimePicker CharacterSpacing on Mac Catalyst",
				FontSize = 18
			};
			var timePicker = new TimePicker
			{
				Format = "hh:mm tt",
				Time = new TimeSpan(11, 0, 0)
			};
			var applySpacingButton = new Button
			{
				Text = "Apply Character Spacing"
			};
			var statusLabel = new Label
			{
				Text = "Character spacing sample",
				FontSize = 18
			};
			var layout = new VerticalStackLayout
			{
				heading,
				timePicker,
				applySpacingButton,
				statusLabel
			};
			var page = new ContentPage
			{
				Title = "Home",
				Content = layout
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var nativePage = Assert.IsAssignableFrom<UIView>(page.Handler.PlatformView);
				var nativeTimePicker = Assert.IsType<UIDatePicker>(timePicker.Handler.PlatformView);
				var nativeApplySpacingButton = Assert.IsAssignableFrom<UIButton>(applySpacingButton.Handler.PlatformView);

				Assert.NotNull(nativeTimePicker.Window);

				var timePickerWidthBefore = nativeTimePicker.IntrinsicContentSize.Width;
				Assert.True(timePickerWidthBefore > 0);

				double observedSpacing = -1;
				timePicker.PropertyChanged += OnTimePickerPropertyChanged;
				applySpacingButton.Clicked += OnApplySpacingClicked;
				nativeApplySpacingButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				applySpacingButton.Clicked -= OnApplySpacingClicked;
				timePicker.PropertyChanged -= OnTimePickerPropertyChanged;

				Assert.Equal(characterSpacing, observedSpacing);
				var timePickerWidthAfter = await CompleteNativeLayoutAsync(nativePage, nativeTimePicker);
				Assert.True(
					timePickerWidthAfter > timePickerWidthBefore + tolerance,
					$"TimePicker native width did not expand for CharacterSpacing 4. Baseline: {timePickerWidthBefore}, actual: {timePickerWidthAfter}.");

				void OnTimePickerPropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == TimePicker.CharacterSpacingProperty.PropertyName)
						observedSpacing = timePicker.CharacterSpacing;
				}

				void OnApplySpacingClicked(object sender, EventArgs args)
				{
					timePicker.CharacterSpacing = characterSpacing;
				}
			});
		}

		static async Task<double> CompleteNativeLayoutAsync(UIView nativePage, UIView measuredView)
		{
			var layoutCompleted = new TaskCompletionSource<double>();
			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				nativePage.SetNeedsLayout();
				nativePage.LayoutIfNeeded();
				layoutCompleted.SetResult(measuredView.IntrinsicContentSize.Width);
			});
			return await layoutCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
		}
	}
}
#endif

