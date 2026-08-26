namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34396, "UI becomes unresponsive when adding 200 Entry children to AbsoluteLayout", PlatformAffected.Android)]
public class Issue34396 : ContentPage
{
	const int EntryCount = 200;

	readonly record struct Widget(double X, double Y, double Width, double Height);

	readonly List<Widget> _items = [];
	readonly Button _clickedButton;
	readonly AbsoluteLayout _canvas;
	readonly Label _statusLabel;
	int _clickCount;

	public Issue34396()
	{
		for (int i = 0; i < EntryCount; i++)
		{
			_items.Add(new Widget(
				(i % 10) * 180,
				(i / 10) * 120,
				160,
				80));
		}

		var addEditorsButton = new Button
		{
			AutomationId = "AddEditorsButton",
			Text = "Add 200 Editors"
		};
		addEditorsButton.Clicked += (_, _) => AddEditors();

		_clickedButton = new Button
		{
			AutomationId = "ResponsivenessButton",
			Text = "Clicked 0"
		};
		_clickedButton.Clicked += (_, _) =>
		{
			_clickCount++;
			_clickedButton.Text = $"Clicked {_clickCount}";
		};

		_statusLabel = new Label
		{
			AutomationId = "BulkAddStatus",
			Text = "Children=0;FinalEntry=False;Responsive=Unknown;Complete=False"
		};

		_canvas = new AbsoluteLayout
		{
			AutomationId = "EntryCanvas",
			WidthRequest = 2000,
			HeightRequest = 3000,
			BackgroundColor = Color.FromArgb("#202020")
		};

		var root = new Grid
		{
			Padding = new Thickness(12),
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		root.Add(new HorizontalStackLayout
		{
			Spacing = 8,
			Children =
			{
				addEditorsButton,
				_clickedButton
			}
		});

		Grid.SetRow(_statusLabel, 1);
		root.Add(_statusLabel);

		var scroller = new ScrollView { Content = _canvas };
		Grid.SetRow(scroller, 2);
		root.Add(scroller);

		Content = root;
	}

	void AddEditors()
	{
		long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();

		for (int i = 0; i < _items.Count; i++)
		{
			Widget item = _items[i];
			var entry = new Entry();

			if (i == EntryCount - 1)
				entry.AutomationId = "FinalEntry";

			_canvas.Children.Add(entry);
			AbsoluteLayout.SetLayoutBounds(entry, new Rect(item.X, item.Y, item.Width, item.Height));
		}

		long elapsedMilliseconds = (long)Math.Ceiling(
			System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
		bool isResponsive = elapsedMilliseconds < 1000;

		Dispatcher.Dispatch(() =>
		{
			bool hasFinalEntry = _canvas.Children.Count == EntryCount &&
				_canvas.Children[^1].AutomationId == "FinalEntry";
			_statusLabel.Text =
				$"Children={_canvas.Children.Count};FinalEntry={hasFinalEntry};Responsive={isResponsive};Complete=True";
		});
	}
}

