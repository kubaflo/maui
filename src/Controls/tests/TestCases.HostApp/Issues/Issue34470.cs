#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34470, "Modal with NavigationPage creates memory leaks", PlatformAffected.iOS)]
public class Issue34470 : ContentPage
{
	const int GarbageCollectionCycles = 5;

	readonly Button _navigateButton;
	readonly Label _modalReadyLabel;
	readonly ContentView _resultHost;
	readonly Label _resultLabel;

	public Issue34470() : this(false)
	{
	}

	Issue34470(bool isModalTarget)
	{
		var instructionLabel = new Label
		{
			Text = "Tap Navigate to present MainPage inside a modal NavigationPage.",
			AutomationId = "InstructionLabel",
			HorizontalTextAlignment = TextAlignment.Center
		};

		_navigateButton = new Button
		{
			Text = "Navigate",
			AutomationId = "NavigateButton"
		};

		_modalReadyLabel = new Label
		{
			Text = "Waiting for modal NavigationPage",
			AutomationId = "ModalReadyLabel",
			HorizontalTextAlignment = TextAlignment.Center,
			IsVisible = false
		};

		_resultLabel = new Label
		{
			Text = "Waiting for handler collection",
			AutomationId = "ResultLabel",
			HorizontalTextAlignment = TextAlignment.Center
		};

		_resultHost = new ContentView
		{
			Content = _resultLabel
		};

		Content = new VerticalStackLayout
		{
			Padding = new Thickness(24),
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				instructionLabel,
				_navigateButton,
				_modalReadyLabel,
				_resultHost
			}
		};

		if (!isModalTarget)
			_navigateButton.Clicked += OnNavigateClicked;
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		_navigateButton.IsEnabled = false;
		var handlerReference = CreateHandlerReference(_navigateButton);
		var modalPage = new Issue34470(true);
		var sourceUnloaded = false;
		var modalLoaded = false;
		var modalPresented = false;
		var collectionStarted = false;

		_resultHost.Content = null;
		modalPage._resultHost.Content = null;
		modalPage._resultHost.Content = _resultLabel;

		Unloaded += OnSourceUnloaded;
		modalPage.Loaded += OnModalLoaded;

		await Navigation.PushModalAsync(new NavigationPage(modalPage));
		modalPresented = true;
		StartCollectionWhenTransitionCompletes();

		void OnSourceUnloaded(object unloadedSender, EventArgs unloadedArgs)
		{
			sourceUnloaded = true;
			StartCollectionWhenTransitionCompletes();
		}

		void OnModalLoaded(object loadedSender, EventArgs loadedArgs)
		{
			modalLoaded = true;
			modalPage._modalReadyLabel.IsVisible = true;
			StartCollectionWhenTransitionCompletes();
		}

		async void StartCollectionWhenTransitionCompletes()
		{
			if (collectionStarted || !modalPresented || !sourceUnloaded || !modalLoaded)
				return;

			collectionStarted = true;
			Unloaded -= OnSourceUnloaded;
			modalPage.Loaded -= OnModalLoaded;
			modalPage._modalReadyLabel.Text = "Modal loaded: True; source unloaded: True";

			for (var cycle = 0; cycle < GarbageCollectionCycles && handlerReference.IsAlive; cycle++)
			{
				await Task.Yield();
				GarbageCollectionHelper.Collect();
			}

			await Task.Yield();
			_resultLabel.Text = $"Outgoing Button handler alive after modal NavigationPage presentation: {handlerReference.IsAlive}; expected False";
		}
	}

	static WeakReference CreateHandlerReference(Button button)
	{
		var handler = button.Handler;
		if (handler is null)
			throw new InvalidOperationException("Navigate Button handler was not created before the modal transition.");

		return new WeakReference(handler);
	}
}
#endif
