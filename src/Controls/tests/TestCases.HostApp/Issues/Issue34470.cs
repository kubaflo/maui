namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34470, "Modal with NavigationPage creates memory leaks", PlatformAffected.iOS)]
public class Issue34470 : ContentPage
{
	public Issue34470()
	{
		Content = new Label
		{
			Text = "Preparing navigation scenario",
			HorizontalTextAlignment = TextAlignment.Center
		};

		Loaded += OnBootstrapLoaded;
	}

	void OnBootstrapLoaded(object sender, EventArgs e)
	{
		Loaded -= OnBootstrapLoaded;

		var currentWindow = Window
			?? throw new InvalidOperationException("The issue page must be attached to a Window.");
		currentWindow.Page = new NavigationPage(new ScenarioPage());
	}

	sealed class ScenarioPage : ContentPage
	{
		readonly Button _navigateButton;
		readonly Label _pageStateLabel;
		readonly Label _handlerStateLabel;
		readonly Label _gcCheckStateLabel;
		WeakReference _priorButtonHandler;

		public ScenarioPage()
		{
			var scenarioLabel = new Label
			{
				Text = "PushModalAsync(new NavigationPage(new ContentPage()))",
				HorizontalTextAlignment = TextAlignment.Center
			};

			_pageStateLabel = new Label
			{
				Text = "Root page ready",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "RootPageState"
			};

			_navigateButton = new Button
			{
				Text = "Navigate",
				AutomationId = "NavigateButton"
			};
			_navigateButton.Clicked += OnNavigateClicked;
			_navigateButton.Loaded += OnNavigateButtonLoaded;

			_handlerStateLabel = new Label
			{
				Text = "HandlerIsAlive=Pending",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "HandlerState"
			};

			_gcCheckStateLabel = new Label
			{
				Text = "GC check pending",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "RootGcCheckState"
			};

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					scenarioLabel,
					_pageStateLabel,
					_navigateButton,
					_handlerStateLabel,
					_gcCheckStateLabel
				}
			};
		}

		ScenarioPage(WeakReference priorButtonHandler)
		{
			_priorButtonHandler = priorButtonHandler;

			_pageStateLabel = new Label
			{
				Text = "Modal page opened",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "ModalPageState"
			};

			var sourceStateLabel = new Label
			{
				Text = "Waiting for source button to unload",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "SourceButtonState"
			};

			_handlerStateLabel = new Label
			{
				Text = "HandlerIsAlive=Pending",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "ModalHandlerState"
			};

			_gcCheckStateLabel = new Label
			{
				Text = "GC check pending",
				HorizontalTextAlignment = TextAlignment.Center,
				AutomationId = "GcCheckState"
			};

			_navigateButton = new Button
			{
				Text = "Navigate"
			};

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 20,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "PushModalAsync(new NavigationPage(new ContentPage()))",
						HorizontalTextAlignment = TextAlignment.Center
					},
					_pageStateLabel,
					sourceStateLabel,
					_navigateButton,
					_handlerStateLabel,
					_gcCheckStateLabel
				}
			};

			SourceStateLabel = sourceStateLabel;
		}

		Label SourceStateLabel { get; }

		void OnNavigateButtonLoaded(object sender, EventArgs e)
		{
			_navigateButton.Loaded -= OnNavigateButtonLoaded;
			_pageStateLabel.Text = "Root button loaded";
		}

		async void OnNavigateClicked(object sender, EventArgs e)
		{
			var handlerReference = new WeakReference(_navigateButton.Handler
				?? throw new InvalidOperationException("The Navigate button must have a handler before navigation."));
			var modalPage = new ScenarioPage(handlerReference);
			_navigateButton.Unloaded += modalPage.OnSourceButtonUnloaded;

			await Navigation.PushModalAsync(new NavigationPage(modalPage));
		}

		void OnSourceButtonUnloaded(object sender, EventArgs e)
		{
			if (sender is Button sourceButton)
				sourceButton.Unloaded -= OnSourceButtonUnloaded;

			SourceStateLabel.Text = "Source Navigate button unloaded";
			Dispatcher.Dispatch(CheckPriorHandler);
		}

		async void CheckPriorHandler()
		{
			var wasCollected = await WaitForGC(_priorButtonHandler);
			_handlerStateLabel.Text = $"HandlerIsAlive={!wasCollected}";
			_gcCheckStateLabel.Text = "GC check complete";
		}

		static async Task<bool> WaitForGC(WeakReference reference)
		{
			for (var cycle = 0; cycle < 40 && reference.IsAlive; cycle++)
			{
				await Task.Yield();
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect(2, GCCollectionMode.Forced, true);
				await Task.Yield();
			}

			return !reference.IsAlive;
		}
	}
}
