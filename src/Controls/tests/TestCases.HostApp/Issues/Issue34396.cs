using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34396, "UI becomes unresponsive when adding 200 Entry children to AbsoluteLayout", PlatformAffected.Android)]
public class Issue34396 : ContentPage
{
	const int EditorCount = 200;

	readonly AbsoluteLayout _editorCanvas;
	readonly Label _resultLabel;
	int _clickCount;

	public Issue34396()
	{
		var addEditorsButton = new Button
		{
			Text = "Add 200 Editors",
			AutomationId = "Issue34396AddEditorsButton"
		};
		addEditorsButton.Clicked += OnAddEditorsClicked;

		var clickedButton = new Button
		{
			Text = "Clicked 0"
		};
		clickedButton.Clicked += (_, _) =>
		{
			_clickCount++;
			clickedButton.Text = $"Clicked {_clickCount}";
		};

		_editorCanvas = new AbsoluteLayout
		{
			WidthRequest = 2000,
			HeightRequest = 3000,
			BackgroundColor = Color.FromArgb("#202020")
		};

		_resultLabel = new Label
		{
			Text = "Count=-1;ElapsedMs=-1",
			AutomationId = "Issue34396Status"
		};

		var root = new Grid
		{
			Padding = 12,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		var toolbar = new HorizontalStackLayout
		{
			Spacing = 8,
			Children =
			{
				addEditorsButton,
				clickedButton
			}
		};

		root.Add(toolbar);
		Grid.SetRow(_resultLabel, 1);
		root.Add(_resultLabel);

		var scroller = new ScrollView { Content = _editorCanvas };
		Grid.SetRow(scroller, 2);
		root.Add(scroller);

		Content = root;
	}

	void OnAddEditorsClicked(object sender, EventArgs e)
	{
		long start = System.Diagnostics.Stopwatch.GetTimestamp();

		for (int i = 0; i < EditorCount; i++)
		{
			var editor = new Entry
			{
				Text = $"Editor {i + 1}"
			};

			_editorCanvas.Children.Add(editor);
			AbsoluteLayout.SetLayoutBounds(
				editor,
				new Rect((i % 10) * 190, 60 + (i / 10) * 140, 180, 120));
		}

		double elapsedMilliseconds =
			(System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000d / System.Diagnostics.Stopwatch.Frequency;
		_resultLabel.Text =
			$"Count={_editorCanvas.Children.Count};ElapsedMs={elapsedMilliseconds.ToString("F1", CultureInfo.InvariantCulture)}";
	}
}

