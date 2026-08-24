#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	int _focusEventCount;
	readonly Label _focusTokenLabel;

	public Issue36412()
	{
		_focusTokenLabel = new Label
		{
			AutomationId = "Issue36412FocusToken",
			Text = "Count=0;Last=None",
			VerticalOptions = LayoutOptions.Center
		};

		var header = new Grid
		{
			Padding = 12,
			ColumnDefinitions =
			[
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			],
			ColumnSpacing = 12
		};
		header.Add(_focusTokenLabel);
		header.Add(new Button
		{
			AutomationId = "Issue36412Check",
			Text = "Check focus"
		}, 1, 0);

		var fields = new VerticalStackLayout
		{
			Padding = 16,
			Spacing = 28
		};

		for (int fieldNumber = 1; fieldNumber <= 15; fieldNumber++)
		{
			var entry = new Entry
			{
				AutomationId = $"Issue36412Field{fieldNumber}",
				Keyboard = Keyboard.Numeric,
				Placeholder = $"Field {fieldNumber}"
			};

			if (fieldNumber is 1 or 7)
			{
				int focusedFieldNumber = fieldNumber;
				entry.Focused += (sender, args) =>
				{
					_focusEventCount++;
					_focusTokenLabel.Text = $"Count={_focusEventCount};Last=Field{focusedFieldNumber}";
				};
			}

			fields.Add(entry);
		}

		var rootGrid = new Grid
		{
			AutomationId = "Issue36412Root",
			RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			]
		};
		rootGrid.Add(header);
		rootGrid.Add(new ScrollView { Content = fields }, 0, 1);

		Content = rootGrid;
	}
}
#endif

