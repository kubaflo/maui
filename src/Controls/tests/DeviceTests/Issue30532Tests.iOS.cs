#if MACCATALYST
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30532")]
public class Issue30532 : ControlsHandlerTestBase
{
	const double ExpectedCharacterSpacing = 10;
	const double CharacterSpacingTolerance = 0.01;

	[Fact]
	public async Task CharacterSpacingUpdatesDisplayedNativeTextAfterAttachment()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<ContentPage, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var timePicker = new TimePicker
		{
			Time = new TimeSpan(11, 0, 0)
		};
		var oracleLabel = new Label
		{
			CharacterSpacing = ExpectedCharacterSpacing
		};
		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				timePicker,
				oracleLabel
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.Equal(0, timePicker.CharacterSpacing);

			var timePickerHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var nativeTimePicker = timePickerHandler.PlatformView;
			Assert.NotNull(nativeTimePicker.Window);

			var initialRun = GetDisplayedTextRuns(nativeTimePicker)
				.OrderByDescending(run => run.Text.Length)
				.FirstOrDefault();

			Assert.False(string.IsNullOrEmpty(initialRun.Text), "The attached native TimePicker did not expose a displayed text run.");
			Assert.InRange(initialRun.CharacterSpacing, -CharacterSpacingTolerance, CharacterSpacingTolerance);

			oracleLabel.Text = initialRun.Text;
			var oracleHandler = Assert.IsType<LabelHandler>(oracleLabel.Handler);
			var oracleUpdated = await AssertHelpers.Wait(
				() => Math.Abs(oracleHandler.PlatformView.AttributedText.GetCharacterSpacing() - ExpectedCharacterSpacing) <= CharacterSpacingTolerance);
			var oracleCharacterSpacing = oracleHandler.PlatformView.AttributedText.GetCharacterSpacing();

			Assert.True(oracleUpdated, $"The Label kerning oracle did not reach {ExpectedCharacterSpacing}. Observed: {oracleCharacterSpacing}.");
			Assert.InRange(
				oracleCharacterSpacing,
				ExpectedCharacterSpacing - CharacterSpacingTolerance,
				ExpectedCharacterSpacing + CharacterSpacingTolerance);

			var propertyChanged = false;
			var notifiedCharacterSpacing = double.NaN;
			timePicker.PropertyChanged += OnTimePickerPropertyChanged;

			timePicker.CharacterSpacing = ExpectedCharacterSpacing;

			Assert.True(propertyChanged, "TimePicker did not raise PropertyChanged for CharacterSpacing.");
			Assert.Equal(ExpectedCharacterSpacing, notifiedCharacterSpacing);
			Assert.Same(nativeTimePicker, timePickerHandler.PlatformView);

			var nativeUpdated = await AssertHelpers.Wait(
				() => Math.Abs(GetCharacterSpacing(nativeTimePicker, initialRun.Text) - ExpectedCharacterSpacing) <= CharacterSpacingTolerance);
			var observedCharacterSpacing = GetCharacterSpacing(nativeTimePicker, initialRun.Text);

			Assert.True(
				nativeUpdated,
				$"TimePicker native character spacing did not update. Initial: {initialRun.CharacterSpacing}; observed: {observedCharacterSpacing}; expected: {ExpectedCharacterSpacing}.");
			Assert.InRange(
				observedCharacterSpacing,
				ExpectedCharacterSpacing - CharacterSpacingTolerance,
				ExpectedCharacterSpacing + CharacterSpacingTolerance);

			timePicker.PropertyChanged -= OnTimePickerPropertyChanged;

			void OnTimePickerPropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == TimePicker.CharacterSpacingProperty.PropertyName)
				{
					propertyChanged = true;
					notifiedCharacterSpacing = timePicker.CharacterSpacing;
				}
			}
		});
	}

	static double GetCharacterSpacing(UIView nativeView, string text)
	{
		var run = GetDisplayedTextRuns(nativeView).FirstOrDefault(candidate => candidate.Text == text);
		return string.IsNullOrEmpty(run.Text) ? double.NaN : run.CharacterSpacing;
	}

	static IEnumerable<(string Text, double CharacterSpacing)> GetDisplayedTextRuns(UIView nativeView)
	{
		if (nativeView is UILabel label && !string.IsNullOrEmpty(label.Text))
			yield return (label.Text, label.AttributedText.GetCharacterSpacing());

		if (nativeView is UITextField textField && !string.IsNullOrEmpty(textField.Text))
			yield return (textField.Text, textField.AttributedText.GetCharacterSpacing());

		foreach (var subview in nativeView.Subviews)
		{
			foreach (var run in GetDisplayedTextRuns(subview))
				yield return run;
		}
	}
}
#endif

