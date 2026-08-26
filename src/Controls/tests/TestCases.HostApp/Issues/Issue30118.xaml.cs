using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30118, "IndicatorView does not visually update when the ItemsSource count changes", PlatformAffected.iOS)]
public partial class Issue30118 : ContentPage
{
	readonly ObservableCollection<string> _items = new() { "Item 1" };
	int _lastIndicatorPosition = -1;

	public Issue30118()
	{
		InitializeComponent();

		carouselView.ItemsSource = _items;
		carouselView.IndicatorView = indicatorView;
		indicatorView.PropertyChanged += OnIndicatorPropertyChanged;

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnIndicatorTargetTapped;
		indicatorTouchTarget.GestureRecognizers.Add(tapGesture);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		countStatus.Text = $"PAGES: {indicatorView.Count}";
		UpdatePositionStatus();
	}

	void OnIncreaseClicked(object sender, EventArgs e)
	{
		if (_items.Count != 1)
			return;

		for (var index = 2; index <= 8; index++)
			_items.Add($"Item {index}");

		countStatus.Text = $"PAGES: {indicatorView.Count}";
		UpdatePositionStatus();
	}

	void OnIndicatorTargetTapped(object sender, TappedEventArgs e)
	{
		targetStatus.Text = "TARGET RECEIVED:";
	}

	void OnIndicatorPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == IndicatorView.PositionProperty.PropertyName)
		{
			_lastIndicatorPosition = indicatorView.Position;
			UpdatePositionStatus();
		}
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
		UpdatePositionStatus();
	}

	void UpdatePositionStatus()
	{
		resultStatus.Text = $"POSITION: {indicatorView.Position}; CALLBACK: {_lastIndicatorPosition}";
	}
}
