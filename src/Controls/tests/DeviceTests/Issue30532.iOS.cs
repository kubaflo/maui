#if MACCATALYST
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category("Issue30532")]
[Category(TestCategory.DatePicker)]
public class Issue30532 : ControlsHandlerTestBase
{
	const double ExpectedCharacterSpacing = 10;
	const double KerningTolerance = 0.01;

	[Fact]
	public async Task CharacterSpacingUpdatesAttachedTimePicker()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var timePicker = new TimePicker
		{
			Time = new TimeSpan(11, 0, 0),
			AutomationId = "TestTimePicker"
		};
		var layout = new VerticalStackLayout
		{
			new Label { Text = "TimePicker with platform-default styling" },
			timePicker,
			new Label { Text = "Character spacing status", AutomationId = "ResultLabel" },
			new Button { Text = "Apply character spacing", AutomationId = "ApplySpacingButton" },
			new Button
			{
				Text = "Check rendered spacing",
				IsVisible = false,
				AutomationId = "CheckResultButton"
			}
		};
		var page = new ContentPage
		{
			Title = "Home",
			Content = layout
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			Assert.Equal(new TimeSpan(11, 0, 0), timePicker.Time);
			Assert.Equal(0, timePicker.CharacterSpacing);

			var handler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var nativePicker = Assert.IsType<UIDatePicker>(handler.PlatformView);
			Assert.NotNull(nativePicker.Window);

			var baselineFields = GetAttributedTextFields(nativePicker);
			Assert.NotEmpty(baselineFields);
			Assert.All(baselineFields, field =>
				Assert.All(GetKerningValues(field), kerning =>
					Assert.True(!kerning.HasValue || Math.Abs(kerning.Value) <= KerningTolerance,
						$"Expected baseline native text segment '{field.AttributedText.Value}' to have no kerning, but found {FormatKerning(kerning)}.")));

			var characterSpacingChanged = false;
			var spacingChanged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			timePicker.PropertyChanged += OnPropertyChanged;

			timePicker.CharacterSpacing = ExpectedCharacterSpacing;

			await spacingChanged.Task.WaitAsync(TimeSpan.FromSeconds(2));
			Assert.True(characterSpacingChanged);
			Assert.Equal(ExpectedCharacterSpacing, timePicker.CharacterSpacing);

			var failureMessage = CreateFailureMessage(nativePicker);
			await AssertEventually(
				() => HasExpectedKerning(nativePicker),
				timeout: 1000,
				interval: 100,
				message: failureMessage);

			var currentHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var currentPicker = Assert.IsType<UIDatePicker>(currentHandler.PlatformView);
			var currentFields = GetAttributedTextFields(currentPicker);
			var sameNativeViews = ReferenceEquals(nativePicker, currentPicker) &&
				baselineFields.Count == currentFields.Count &&
				baselineFields.Zip(currentFields).All(pair => ReferenceEquals(pair.First, pair.Second));

			Assert.True(sameNativeViews, CreateFailureMessage(currentPicker));
			Assert.All(currentFields, field =>
				Assert.All(GetKerningValues(field), kerning =>
					Assert.True(
						kerning.HasValue && Math.Abs(kerning.Value - ExpectedCharacterSpacing) <= KerningTolerance,
						CreateFailureMessage(currentPicker))));

			timePicker.PropertyChanged -= OnPropertyChanged;

			void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == nameof(TimePicker.CharacterSpacing))
				{
					characterSpacingChanged = true;
					spacingChanged.TrySetResult(true);
				}
			}
		});
	}

	static List<UITextField> GetAttributedTextFields(UIView root)
	{
		var fields = new List<UITextField>();
		AddAttributedTextFields(root, fields);
		return fields;
	}

	static void AddAttributedTextFields(UIView view, List<UITextField> fields)
	{
		if (view is UITextField textField && textField.AttributedText is { Length: > 0 })
			fields.Add(textField);

		foreach (var child in view.Subviews)
			AddAttributedTextFields(child, fields);
	}

	static List<double?> GetKerningValues(UITextField textField)
	{
		var attributedText = textField.AttributedText;
		var values = new List<double?>((int)attributedText.Length);

		for (nint index = 0; index < attributedText.Length; index++)
		{
			var value = attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, index, out _);
			values.Add(value is NSNumber number ? number.DoubleValue : null);
		}

		return values;
	}

	static bool HasExpectedKerning(UIDatePicker nativePicker)
	{
		var fields = GetAttributedTextFields(nativePicker);
		return fields.Count > 0 &&
			fields.All(field => GetKerningValues(field).All(kerning =>
				kerning.HasValue && Math.Abs(kerning.Value - ExpectedCharacterSpacing) <= KerningTolerance));
	}

	static string CreateFailureMessage(UIDatePicker nativePicker)
	{
		var fields = GetAttributedTextFields(nativePicker);
		var details = fields.Count == 0
			? "<no non-empty attributed text segments>"
			: string.Join("; ", fields.Select(field =>
				$"text='{field.AttributedText.Value}', kerning=[{string.Join(", ", GetKerningValues(field).Select(FormatKerning))}]"));

		return $"TimePicker CharacterSpacing=10 was not applied to native text segments: {details}; expected 10 +/- {KerningTolerance}.";
	}

	static string FormatKerning(double? value) => value?.ToString("G") ?? "<missing>";
}
#endif

