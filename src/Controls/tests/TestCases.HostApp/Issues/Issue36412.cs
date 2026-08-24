namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	readonly Label _focusStatusLabel;
	readonly Label _counterStatusLabel;
	int _field1FocusedCount;
	int _field1UnfocusedCount;
	int _field8FocusedCount;
	string _focusOwner = "None";

	public Issue36412()
	{
		_focusStatusLabel = new Label
		{
			AutomationId = "FocusStatusLabel",
			Text = "No field focused"
		};

		_counterStatusLabel = new Label
		{
			AutomationId = "CounterStatusLabel",
			Text = GetCounterStatus(),
			FontAttributes = FontAttributes.Bold
		};

		var entries = new VerticalStackLayout
		{
			Spacing = 20
		};
		var scrollView = new ScrollView
		{
			Content = entries
		};

		for (int fieldNumber = 1; fieldNumber <= 15; fieldNumber++)
		{
			var entry = new Entry
			{
				AutomationId = $"Field{fieldNumber}",
				Placeholder = $"Field {fieldNumber}",
				Keyboard = Keyboard.Numeric
			};

			if (fieldNumber == 1)
			{
				entry.Focused += OnField1Focused;
				entry.Unfocused += OnField1Unfocused;
			}
			else if (fieldNumber == 8)
			{
				entry.Focused += OnField8Focused;
			}

			entries.Children.Add(entry);
		}

		Content = new Grid
		{
			Padding = 12,
			RowSpacing = 6,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				_focusStatusLabel,
				_counterStatusLabel,
				scrollView
			}
		};

		Grid.SetRow(_counterStatusLabel, 1);
		Grid.SetRow(scrollView, 2);
	}

	void OnField1Focused(object sender, FocusEventArgs e)
	{
		_field1FocusedCount++;
		_focusOwner = "Field1";
		_focusStatusLabel.Text = "Field 1 focused";
		UpdateCounterStatus();
	}

	void OnField1Unfocused(object sender, FocusEventArgs e)
	{
		_field1UnfocusedCount++;
		if (_focusOwner == "Field1")
			_focusOwner = "None";
		UpdateCounterStatus();
	}

	void OnField8Focused(object sender, FocusEventArgs e)
	{
		_field8FocusedCount++;
		_focusOwner = "Field8";
		_focusStatusLabel.Text = "Field 8 focused";
		UpdateCounterStatus();
	}

	void UpdateCounterStatus()
	{
		_counterStatusLabel.Text = GetCounterStatus();
	}

	string GetCounterStatus()
	{
		return $"F1={_field1FocusedCount};U1={_field1UnfocusedCount};F8={_field8FocusedCount};Owner={_focusOwner}";
	}
}

