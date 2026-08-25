using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	public Issue36412()
	{
		var lastFocusedIndex = new Label
		{
			AutomationId = "LastFocusedIndex",
			Text = "-1"
		};
		var focusEventCount = new Label
		{
			AutomationId = "FocusEventCount",
			Text = "0"
		};
		var fields = new VerticalStackLayout();
		var eventCount = 0;

		for (var index = 1; index <= 15; index++)
		{
			var fieldIndex = index;
			var entry = new Entry
			{
				AutomationId = $"Field{fieldIndex}",
				Placeholder = $"Field {fieldIndex}",
				Keyboard = Keyboard.Numeric
			};
			entry.Focused += (_, _) =>
			{
				eventCount++;
				lastFocusedIndex.Text = fieldIndex.ToString(CultureInfo.InvariantCulture);
				focusEventCount.Text = eventCount.ToString(CultureInfo.InvariantCulture);
			};
			fields.Children.Add(entry);
		}

		Content = new Grid
		{
			Children =
			{
				new ScrollView
				{
					Content = fields
				},
				new VerticalStackLayout
				{
					AutomationId = "FocusTelemetry",
					HorizontalOptions = LayoutOptions.End,
					VerticalOptions = LayoutOptions.Start,
					WidthRequest = 150,
					Children =
					{
						lastFocusedIndex,
						focusEventCount
					}
				}
			}
		};
	}
}

