namespace Maui.Controls.Sample.Issues;

#if IOS
[Issue(IssueTracker.Github, 34470, "Modal with NavigationPage creates memory leaks", PlatformAffected.iOS)]
public class Issue34470 : ContentPage
{
	static Label s_sharedStateLabel;
	static WeakReference s_previousButtonHandler;

	readonly Grid _rootLayout;
	readonly Label _stateLabel;
	readonly bool _isModalPage;

	public Issue34470()
	{
		var sharedStateLabel = s_sharedStateLabel;
		s_sharedStateLabel = null;
		_isModalPage = sharedStateLabel is not null;

		var pageMarkerLabel = new Label
		{
			Text = _isModalPage ? "Modal page" : "Main page",
			TextColor = Colors.White,
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			AutomationId = _isModalPage ? "ModalPageMarker" : "RootPageMarker"
		};

		var initialStateLabel = new Label
		{
			Text = "CallbackToken=0; IsAlive=Pending",
			TextColor = Colors.White,
			FontSize = 18,
			HorizontalOptions = LayoutOptions.Center
		};

		_stateLabel = sharedStateLabel ?? initialStateLabel;
		if (!_isModalPage)
			_stateLabel.AutomationId = "CollectionState";

		var navigateButton = new Button
		{
			Text = "Navigate",
			AutomationId = "NavigateButton"
		};
		navigateButton.Clicked += OnNavigateClicked;

		_rootLayout = new Grid
		{
			Padding = new Thickness(24),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 20,
			VerticalOptions = LayoutOptions.Center
		};

		_rootLayout.Add(pageMarkerLabel, 0, 0);
		_rootLayout.Add(_stateLabel, 0, 1);
		_rootLayout.Add(navigateButton, 0, 2);

		BackgroundColor = Color.FromArgb("#24135F");
		Content = _rootLayout;

		if (_isModalPage)
			Loaded += OnModalPageLoaded;
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		s_previousButtonHandler = CaptureHandler((Button)sender);
		s_sharedStateLabel = _stateLabel;
		_rootLayout.Remove(_stateLabel);

		await Navigation.PushModalAsync(new NavigationPage(new Issue34470()));
	}

	void OnModalPageLoaded(object sender, EventArgs e)
	{
		Loaded -= OnModalPageLoaded;
		Dispatcher.Dispatch(CheckPreviousButtonHandler);
	}

	async void CheckPreviousButtonHandler()
	{
		var reference = s_previousButtonHandler
			?? throw new InvalidOperationException("The previous ButtonHandler was not captured.");

		await Issue34470AssertionExtensions.WaitForGC(reference);
		_stateLabel.Text = $"CallbackToken=1; IsAlive={reference.IsAlive}";
	}

	static WeakReference CaptureHandler(Button button)
	{
		return new WeakReference(button.Handler
			?? throw new InvalidOperationException("The Navigate button has no handler."));
	}
}

static class Issue34470AssertionExtensions
{
	public static async Task WaitForGC(WeakReference reference)
	{
		for (int cycle = 0; cycle < 5 && reference.IsAlive; cycle++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await Task.Yield();
		}
	}
}
#endif
