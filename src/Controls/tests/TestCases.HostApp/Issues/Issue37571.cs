#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37571, "Looped CarouselView stops responding after one traversal", PlatformAffected.Android)]
public class Issue37571 : ContentPage
{
	static readonly int[] ExpectedPositions = [4, 0, 1, 2, 3, 4, 0, 1, 2, 3];
	readonly List<int> _observedPositions = [];
	readonly Label _positionLabel;
	readonly Label _currentItemLabel;
	readonly Label _selectedItemLabel;
	readonly Label _resultLabel;
	int _lastPosition = 3;

	public Issue37571()
	{
		Title = "CarouselView Core Gallery";

		var items = Enumerable.Range(0, 5)
			.Select(index => new CarouselItem(index))
			.ToList();

		var carousel = new CarouselView1
		{
			AutomationId = "TheCarouselView",
			ItemsSource = items,
			Loop = true,
			Position = 3,
			ItemTemplate = new DataTemplate(() =>
			{
				var image = new Image
				{
					Source = "oasis.jpg",
					Aspect = Aspect.AspectFit,
					InputTransparent = true
				};
				image.SetBinding(AutomationIdProperty, nameof(CarouselItem.Title));

				var indexLabel = new Label
				{
					FontSize = 14,
					TextColor = Colors.Black,
					HorizontalOptions = LayoutOptions.Start,
					VerticalOptions = LayoutOptions.Center
				};
				indexLabel.SetBinding(Label.TextProperty, nameof(CarouselItem.Index));

				var titleLabel = new Label
				{
					FontSize = 14,
					TextColor = Colors.Black,
					HorizontalOptions = LayoutOptions.End,
					VerticalOptions = LayoutOptions.Center
				};
				titleLabel.SetBinding(Label.TextProperty, nameof(CarouselItem.Title));

				var itemGrid = new Grid
				{
					RowDefinitions =
					{
						new RowDefinition(GridLength.Star),
						new RowDefinition(GridLength.Auto)
					}
				};
				itemGrid.Add(image);
				itemGrid.Add(indexLabel, row: 1);
				itemGrid.Add(titleLabel, row: 1);

				return new Border
				{
					Padding = 10,
					HeightRequest = 100,
					WidthRequest = 200,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					BackgroundColor = Colors.Yellow,
					Content = itemGrid
				};
			})
		};

		_positionLabel = CreateValueLabel("lblPosition", "3");
		_currentItemLabel = CreateValueLabel("lblCurrentItem", "3");
		_selectedItemLabel = CreateValueLabel("lblSelected", "3");
		_resultLabel = new Label
		{
			AutomationId = "ResultStatus",
			Text = "NO BUG:",
			VerticalOptions = LayoutOptions.Center
		};
		var checkNavigationButton = new Button
		{
			AutomationId = "CheckNavigation",
			Text = "Check navigation"
		};
		checkNavigationButton.Clicked += OnCheckNavigationClicked;

		var statusLayout = new HorizontalStackLayout
		{
			Children =
			{
				_resultLabel,
				checkNavigationButton
			}
		};

		var buttonLayout = new HorizontalStackLayout
		{
			HorizontalOptions = LayoutOptions.Center,
			Children =
			{
				CreateGalleryButton("<", true),
				CreateGalleryButton("Remove 3", false),
				CreateGalleryButton(">", true)
			}
		};

		var root = new Grid
		{
			Margin = new Thickness(0, 0, 0, 10),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			ColumnDefinitions =
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			}
		};

		root.AddWithSpan(statusLayout, columnSpan: 2);
		AddValueRow(root, "Position:", _positionLabel, 1);
		AddValueRow(root, "CurrentItem :", _currentItemLabel, 2);
		AddValueRow(root, "Selected: ", _selectedItemLabel, 3);
		root.AddWithSpan(buttonLayout, row: 4, columnSpan: 2);
		root.AddWithSpan(carousel, row: 5, columnSpan: 2);

		carousel.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName != CarouselView.PositionProperty.PropertyName ||
				carousel.Position == _lastPosition)
				return;

			_lastPosition = carousel.Position;
			_observedPositions.Add(carousel.Position);
			var positionText = carousel.Position.ToString();
			_positionLabel.Text = positionText;
			_currentItemLabel.Text = positionText;
			_selectedItemLabel.Text = positionText;
		};

		Content = root;
	}

	void OnCheckNavigationClicked(object sender, EventArgs args)
	{
		_resultLabel.Text = _observedPositions.SequenceEqual(ExpectedPositions)
			? "NO BUG:"
			: "BUG REPRODUCED:";

		if (sender is Button button)
			button.Text = "Checked";
	}

	static Label CreateValueLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	static Button CreateGalleryButton(string text, bool bold) =>
		new()
		{
			Text = text,
			FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
			BackgroundColor = Colors.LightGray,
			TextColor = Colors.Black
		};

	static void AddValueRow(Grid grid, string caption, Label value, int row)
	{
		grid.Add(new Label { Text = caption }, row: row);
		grid.Add(value, column: 1, row: row);
	}

	public class CarouselItem
	{
		public CarouselItem(int index)
		{
			Index = index;
		}

		public int Index { get; }

		public string Title => $"CarouselItem{Index}";
	}
}
#endif
