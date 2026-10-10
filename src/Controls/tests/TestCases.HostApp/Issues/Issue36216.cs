#if IOS
using Microsoft.Maui.Devices.Sensors;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36216, "Accelerometer ReadingChanged retains stopped page subscribers", PlatformAffected.iOS)]
public class Issue36216 : NavigationPage
{
	public Issue36216() : base(new Issue36216MainPage())
	{
	}
}

sealed class Issue36216MainPage : ContentPage
{
	readonly Button _createPageButton;
	readonly Button _checkPageButton;
	readonly Label _lifecycleStatusLabel;
	readonly Label _retainedPagesLabel;
	WeakReference _removedSubscriberPage = new(new object());
	int _appearingCount;
	int _disappearingCount;

	public Issue36216MainPage()
	{
		_lifecycleStatusLabel = new Label
		{
			AutomationId = "LifecycleStatus",
			Text = "Lifecycle: pending"
		};

		_createPageButton = new Button
		{
			AutomationId = "CreatePageButton",
			Text = "Create and remove page"
		};
		_createPageButton.Clicked += CreateAndRemovePage;

		_checkPageButton = new Button
		{
			AutomationId = "CheckPageButton",
			IsEnabled = false,
			IsVisible = false,
			Text = "Check retained page"
		};
		_checkPageButton.Clicked += CheckRetainedPage;

		_retainedPagesLabel = new Label
		{
			AutomationId = "RetainedPages",
			Text = "Retained pages: not checked"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					Text = "Accelerometer subscriber retention"
				},
				new Label
				{
					Text = "Create a page that subscribes to Accelerometer.ReadingChanged, remove it from navigation, then check whether the page can be collected."
				},
				_lifecycleStatusLabel,
				_createPageButton,
				_checkPageButton,
				_retainedPagesLabel
			}
		};
	}

	async void CreateAndRemovePage(object sender, EventArgs e)
	{
		_createPageButton.IsEnabled = false;
		_checkPageButton.IsEnabled = false;
		_checkPageButton.IsVisible = false;
		_lifecycleStatusLabel.Text = "Lifecycle: pending";
		_retainedPagesLabel.Text = "Retained pages: not checked";

		_removedSubscriberPage = await PushAndPopSubscriberPage(
			Navigation,
			() => _appearingCount++,
			() => _disappearingCount++);

		_lifecycleStatusLabel.Text = $"Lifecycle: appearing={_appearingCount}, disappearing={_disappearingCount}";
		_checkPageButton.IsEnabled = true;
		_checkPageButton.IsVisible = true;
	}

	async void CheckRetainedPage(object sender, EventArgs e)
	{
		_checkPageButton.IsEnabled = false;
		_retainedPagesLabel.Text = "Retained pages: checking";

		var collectionCheck = GarbageCollectionHelper.WaitForGC(5000, _removedSubscriberPage);
		await Task.WhenAny(collectionCheck);

		var collectionException = collectionCheck.Exception;
		if (collectionException is not null)
		{
			var collectionFailure = collectionException.GetBaseException();
			if (collectionFailure.Message != "Assertion timed out")
				throw collectionFailure;
		}

		var retainedCount = _removedSubscriberPage.IsAlive ? 1 : 0;
		_retainedPagesLabel.Text = $"Popped subscriber pages retained: {retainedCount} of 1";
	}

	static async Task<WeakReference> PushAndPopSubscriberPage(
		INavigation navigation,
		Action appearing,
		Action disappearing)
	{
		var subscriberPage = new Issue36216SubscriberPage(appearing, disappearing);
		var subscriberReference = new WeakReference(subscriberPage);

		await navigation.PushAsync(subscriberPage, false);
		await navigation.PopAsync(false);

		return subscriberReference;
	}
}

sealed class Issue36216SubscriberPage : ContentPage
{
	readonly Action _appearing;
	readonly Action _disappearing;
	readonly Issue36216SubscriberViewModel _subscriberViewModel = new();

	public Issue36216SubscriberPage(Action appearing, Action disappearing)
	{
		_appearing = appearing;
		_disappearing = disappearing;
		BindingContext = _subscriberViewModel;
		Content = new Label
		{
			Text = "Accelerometer subscriber page"
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_appearing();
		Accelerometer.ReadingChanged += OnReadingChanged;
	}

	protected override void OnDisappearing()
	{
		_disappearing();

		if (Accelerometer.IsMonitoring)
			Accelerometer.Stop();

		base.OnDisappearing();
	}

	void OnReadingChanged(object sender, AccelerometerChangedEventArgs e)
	{
		_subscriberViewModel.LastReading = e.Reading.Acceleration.X;
		_subscriberViewModel.Payload[0] = 1;
	}
}

sealed class Issue36216SubscriberViewModel
{
	public byte[] Payload { get; } = new byte[1024 * 1024];

	public float LastReading { get; set; }
}
#endif

