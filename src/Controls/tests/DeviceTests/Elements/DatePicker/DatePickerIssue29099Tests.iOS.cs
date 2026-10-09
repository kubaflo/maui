#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue29099")]
public class Issue29099 : ControlsHandlerTestBase
{
	[Fact]
	public async Task FormatControlsVisibleDateSegmentOrder()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<ContentPage, PageHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
			});
		});

		var testDate = new DateTime(2001, 2, 3);
		var datePicker = new DatePicker
		{
			Date = testDate,
			Format = "dd/MM/yyyy"
		};
		var page = new ContentPage
		{
			Title = "DatePicker format",
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 20,
							Text = "DatePicker Format on Mac Catalyst"
						},
						new Label { Text = "Expected custom format: 03/02/2001" },
						new Label { Text = "Actual DatePicker display:" },
						datePicker,
						new Button { Text = "Inspect DatePicker display" },
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							Text = "The displayed date should use dd/MM/yyyy."
						}
					}
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var observedSegments = await ObserveVisibleSegments(datePicker);
			var expectedSegments = testDate
				.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
				.Split('/')
				.Select(segment => int.Parse(segment, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
				.ToArray();
			var normalizedObservedSegments = NormalizeSegments(observedSegments);

			Assert.True(
				normalizedObservedSegments.SequenceEqual(expectedSegments),
				$"DatePicker Format mismatch: native segments were [{string.Join(", ", normalizedObservedSegments)}], " +
				$"expected [{string.Join(", ", expectedSegments)}] for Format dd/MM/yyyy.");
		});
	}

	static async Task<string[]> ObserveVisibleSegments(DatePicker datePicker)
	{
		var handler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
		Assert.Same(datePicker, handler.VirtualView);

		var platformView = Assert.IsType<UIDatePicker>(handler.PlatformView);
		Assert.NotNull(platformView.Window);

		string[] observedSegments = null;
		await AssertEventually(
			() =>
			{
				var segments = GetTextFields(platformView)
					.Select(field => new
					{
						field.Text,
						Frame = field.ConvertRectToView(field.Bounds, platformView),
						field.Hidden,
						field.Alpha
					})
					.Where(segment =>
						!segment.Hidden &&
						segment.Alpha > 0 &&
						segment.Frame.Width > 0 &&
						segment.Frame.Height > 0 &&
						segment.Frame.Right > platformView.Bounds.Left &&
						segment.Frame.Left < platformView.Bounds.Right &&
						segment.Frame.Bottom > platformView.Bounds.Top &&
						segment.Frame.Top < platformView.Bounds.Bottom &&
						int.TryParse(segment.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
					.OrderBy(segment => segment.Frame.Left)
					.Select(segment => segment.Text.Trim())
					.ToArray();

				if (segments.Length != 3)
					return false;

				observedSegments = segments;
				return true;
			},
			message: "The attached DatePicker did not expose three visible, measured native date segments.");

		Assert.NotNull(observedSegments);
		return observedSegments;
	}

	static IEnumerable<UITextField> GetTextFields(UIView view)
	{
		foreach (var subview in view.Subviews)
		{
			if (subview is UITextField textField)
				yield return textField;

			foreach (var nestedField in GetTextFields(subview))
				yield return nestedField;
		}
	}

	static string[] NormalizeSegments(IEnumerable<string> segments) =>
		segments
			.Select(segment => int.Parse(segment, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
			.ToArray();
}
#endif

