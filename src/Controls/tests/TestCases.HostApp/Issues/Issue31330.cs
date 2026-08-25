#if ANDROID
using Microsoft.Maui.Layouts;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31330, "Rectangle renders as a thin line for small fractional heights", PlatformAffected.Android)]
public class Issue31330 : ContentPage
{
	const double CanvasWidth = 3370;
	const double CanvasHeight = 2383;
	const double ShapeWidth = 20;
	const double ShapeHeight = 1.2;

	readonly AbsoluteLayout _shapeLayout;
	readonly ScrollView _shapeScrollView;
	readonly Label _statusLabel;

	public Issue31330()
	{
		_shapeScrollView = new ScrollView
		{
			Orientation = ScrollOrientation.Both,
			VerticalScrollBarVisibility = ScrollBarVisibility.Always,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Always
		};

		var canvas = new Grid
		{
			WidthRequest = CanvasWidth,
			HeightRequest = CanvasHeight,
			BackgroundColor = Colors.LightGray
		};

		_shapeLayout = new AbsoluteLayout();
		canvas.Children.Add(_shapeLayout);

		var addButton = new Button
		{
			Text = "Add Rectangle",
			AutomationId = "AddRectangle",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start
		};
		addButton.Clicked += OnAddRectangleClicked;

		_statusLabel = new Label
		{
			Text = "Waiting",
			AutomationId = "Issue31330Status",
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Center
		};

		var header = new Grid();
		header.Children.Add(addButton);
		header.Children.Add(_statusLabel);

		var mainLayout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		mainLayout.Children.Add(header);

		_shapeScrollView.Content = canvas;
		mainLayout.Children.Add(_shapeScrollView);
		Grid.SetRow(_shapeScrollView, 1);

		Content = mainLayout;
	}

	async void OnAddRectangleClicked(object sender, EventArgs e)
	{
		const double boxX = (CanvasWidth / 2) - ShapeWidth - 50;
		const double rectangleX = (CanvasWidth / 2) + 50;
		const double shapeY = (CanvasHeight / 2) - (ShapeHeight / 2);

		var box = new BoxView
		{
			BackgroundColor = Colors.Red,
			AutomationId = "RedBoxView"
		};
		AbsoluteLayout.SetLayoutBounds(box, new Rect(boxX, shapeY, ShapeWidth, ShapeHeight));
		AbsoluteLayout.SetLayoutFlags(box, AbsoluteLayoutFlags.None);

		var rectangle = new Microsoft.Maui.Controls.Shapes.Rectangle
		{
			BackgroundColor = Colors.Blue,
			AutomationId = "BlueRectangle"
		};
		AbsoluteLayout.SetLayoutBounds(rectangle, new Rect(rectangleX, shapeY, ShapeWidth, ShapeHeight));
		AbsoluteLayout.SetLayoutFlags(rectangle, AbsoluteLayoutFlags.None);

		_shapeLayout.Children.Add(box);
		_shapeLayout.Children.Add(rectangle);
		_shapeLayout.Children.Add(CreateComparisonLabel("Red BoxView", boxX - 30, shapeY - 35));
		_shapeLayout.Children.Add(CreateComparisonLabel("Blue Rectangle", rectangleX - 40, shapeY - 35));

		await _shapeScrollView.ScrollToAsync(1500, 1050, false);
		_statusLabel.Text = "Controls added";
	}

	static Label CreateComparisonLabel(string text, double x, double y)
	{
		var label = new Label
		{
			Text = text,
			TextColor = Colors.Black,
			BackgroundColor = Colors.White,
			FontSize = 14
		};
		AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, 130, 30));
		AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.None);
		return label;
	}
}
#endif

