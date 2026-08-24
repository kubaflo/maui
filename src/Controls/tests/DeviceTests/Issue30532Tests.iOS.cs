#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.DatePicker)]
[Category("Issue30532")]
public class Issue30532 : ControlsHandlerTestBase
{
	[Fact]
	public async Task CharacterSpacingIsAppliedToDisplayedTimePickerSegments()
	{
		const double expectedCharacterSpacing = 10;
		const double tolerance = 0.01;
		var defaultLayoutGeneration = -1;
		var spacedLayoutGeneration = -1;
		var defaultTimePicker = new TimePicker
		{
			Time = new TimeSpan(11, 0, 0)
		};
		var spacedTimePicker = new TimePicker
		{
			Time = new TimeSpan(11, 0, 0),
			CharacterSpacing = expectedCharacterSpacing
		};

		defaultTimePicker.SizeChanged += (_, _) =>
			defaultLayoutGeneration = defaultLayoutGeneration < 0 ? 1 : defaultLayoutGeneration + 1;
		spacedTimePicker.SizeChanged += (_, _) =>
			spacedLayoutGeneration = spacedLayoutGeneration < 0 ? 1 : spacedLayoutGeneration + 1;

		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label { Text = "Default character spacing" },
					defaultTimePicker,
					new Label { Text = "CharacterSpacing = 10" },
					spacedTimePicker,
					new Label { Text = "Waiting for layout measurements" },
					new Label { Text = "Character spacing result", FontAttributes = FontAttributes.Bold },
					new Button { Text = "Check character spacing" }
				}
			}
		};

		EnsureHandlerCreated(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddMauiControlsHandlers()));

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			await AssertEventually(
				() => defaultLayoutGeneration > 0 && spacedLayoutGeneration > 0,
				message: "Both TimePickers must complete layout after attachment.");

			var defaultHandler = Assert.IsType<TimePickerHandler>(defaultTimePicker.Handler);
			var spacedHandler = Assert.IsType<TimePickerHandler>(spacedTimePicker.Handler);
			var defaultPicker = Assert.IsType<UIDatePicker>(defaultHandler.PlatformView);
			var spacedPicker = Assert.IsType<UIDatePicker>(spacedHandler.PlatformView);

			Assert.NotNull(defaultPicker.Window);
			Assert.NotNull(spacedPicker.Window);

			var defaultSegments = GetVisibleSegments(defaultPicker);
			var spacedSegments = GetVisibleSegments(spacedPicker);

			Assert.NotEmpty(defaultSegments);
			Assert.NotEmpty(spacedSegments);
			Assert.Equal(defaultSegments.Count, spacedSegments.Count);

			for (var index = 0; index < defaultSegments.Count; index++)
			{
				var defaultSegment = defaultSegments[index];
				var spacedSegment = spacedSegments[index];
				var defaultText = defaultSegment.Text;
				var spacedText = spacedSegment.Text;

				Assert.False(string.IsNullOrEmpty(defaultText));
				Assert.False(string.IsNullOrEmpty(spacedText));

				var defaultSpacing = defaultSegment.AttributedText.GetCharacterSpacing();
				Assert.True(
					Math.Abs(defaultSpacing) <= tolerance,
					$"Default TimePicker segment '{defaultText}' had character spacing {defaultSpacing}; expected 0 within tolerance {tolerance}.");

				var spacedSpacing = spacedSegment.AttributedText.GetCharacterSpacing();
				Assert.True(
					Math.Abs(spacedSpacing - expectedCharacterSpacing) <= tolerance,
					$"Issue30532: expected native TimePicker segment character spacing 10, but segment '{spacedText}' measured {spacedSpacing} with tolerance {tolerance}.");
			}
		});
	}

	static List<UITextField> GetVisibleSegments(UIView root)
	{
		var segments = new List<UITextField>();
		AddVisibleSegments(root, segments);
		return segments;
	}

	static void AddVisibleSegments(UIView view, List<UITextField> segments)
	{
		if (view.Hidden || view.Alpha <= 0)
			return;

		if (view is UITextField textField && !string.IsNullOrEmpty(textField.Text))
			segments.Add(textField);

		foreach (var subview in view.Subviews)
			AddVisibleSegments(subview, segments);
	}
}
#endif

