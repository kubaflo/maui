using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.WinRT)]
public partial class Issue32587 : ContentPage
{
	int _tapCount;

	public Issue32587()
	{
		InitializeComponent();

		Issue32587GestureBoundsView.LoadedBoundsObserved += OnLoadedBoundsObserved;
		Issue32587GestureBoundsView.TappedBoundsObserved += OnTappedBoundsObserved;
		IssueItems.ItemsSource = new[] { "Direct ContentView item" };
	}

	void OnLoadedBoundsObserved(double width, double height)
	{
		LoadedBoundsLabel.Text = FormatBounds("Loaded", width, height);
		ReadyLabel.Text = "Direct ContentView is loaded";
	}

	void OnTappedBoundsObserved(double width, double height)
	{
		_tapCount++;
		TapBoundsLabel.Text = FormatBounds("Inside tap", width, height);
		ResultLabel.Text = width > 0 && height > 0
			? "Bounds are positive"
			: "Bounds are invalid";
		TapCountLabel.Text = $"Tap handler fired: {_tapCount}";
	}

	protected override void OnDisappearing()
	{
		Issue32587GestureBoundsView.LoadedBoundsObserved -= OnLoadedBoundsObserved;
		Issue32587GestureBoundsView.TappedBoundsObserved -= OnTappedBoundsObserved;
		base.OnDisappearing();
	}

	static string FormatBounds(string prefix, double width, double height) =>
		$"{prefix} Width={width.ToString("0.###", CultureInfo.InvariantCulture)}, Height={height.ToString("0.###", CultureInfo.InvariantCulture)}";
}

public class Issue32587GestureBoundsView : ContentView
{
	public static event Action<double, double> LoadedBoundsObserved = delegate { };
	public static event Action<double, double> TappedBoundsObserved = delegate { };

	public Issue32587GestureBoundsView()
	{
		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		GestureRecognizers.Add(tapGesture);
		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		LoadedBoundsObserved(Width, Height);
	}

	void OnTapped(object sender, TappedEventArgs e)
	{
		TappedBoundsObserved(Width, Height);
	}
}
