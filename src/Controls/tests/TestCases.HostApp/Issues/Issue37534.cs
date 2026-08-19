#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37534, "WebView CanGoBack remains true after returning to the initial document", PlatformAffected.UWP)]
public class Issue37534 : Shell
{
	public Issue37534()
	{
		var home = new FlyoutItem
		{
			Title = "Home",
			Items =
			{
				new ShellContent
				{
					Title = "Home",
					Content = new ContentPage
					{
						Title = "Home",
						Content = new Label
						{
							Text = "Open Help from the flyout.",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						}
					}
				}
			}
		};

		var help = new FlyoutItem
		{
			Title = "Help",
			Items =
			{
				new ShellContent
				{
					Title = "Help",
					ContentTemplate = new DataTemplate(() => new Issue37534HelpPage())
				}
			}
		};

		Items.Add(home);
		Items.Add(help);
		CurrentItem = home;
		Loaded += (_, _) => FlyoutIsPresented = true;
	}
}

sealed class Issue37534HelpPage : ContentPage
{
	const string HelpMarkup = """
		<!doctype html>
		<html>
		<body>
		  <h1 id="help">Help</h1>
		  <p>This is the initial help page.</p>
		  <a href="#index">Show index</a>
		  <div style="height: 500px"></div>
		  <h2 id="index">Index</h2>
		  <p>This is the linked index.</p>
		</body>
		</html>
		""";

	readonly WebView _helpWebView;
	readonly Label _navigationState;
	readonly Label _historyState;
	readonly Label _repeatedBackState;
	readonly string _identityToken = Guid.NewGuid().ToString("N");
	int _navigationSequence = -1;
	int _backRequests;
	bool _awaitingFirstBack;

	public Issue37534HelpPage()
	{
		Title = "Help";

		_navigationState = new Label
		{
			Text = $"Navigation=-1; Page=Unobserved; WindowVisible=False; HelpSelected=False; Identity={_identityToken}",
			AutomationId = "Issue37534NavigationState"
		};
		_historyState = new Label
		{
			Text = "First back: Unobserved",
			AutomationId = "Issue37534HistoryState"
		};
		_repeatedBackState = new Label
		{
			Text = "Repeated back: Unobserved",
			AutomationId = "Issue37534RepeatedBackState"
		};
		_helpWebView = new WebView
		{
			AutomationId = "Issue37534HelpWebView",
			Source = new HtmlWebViewSource { Html = HelpMarkup }
		};
		_helpWebView.Navigated += OnWebViewNavigated;

		var backItem = new ToolbarItem
		{
			Text = "Back",
			AutomationId = "Issue37534BackButton"
		};
		backItem.Clicked += OnBackClicked;
		ToolbarItems.Add(backItem);

		Content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				_navigationState,
				_historyState,
				_repeatedBackState,
				_helpWebView
			}
		};

		Grid.SetRow(_navigationState, 0);
		Grid.SetRow(_historyState, 1);
		Grid.SetRow(_repeatedBackState, 2);
		Grid.SetRow(_helpWebView, 3);
	}

	void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
	{
		_navigationSequence++;
		bool showingIndex = e.Url?.Contains("#index", StringComparison.Ordinal) == true;
		var currentWindow = Window;
		bool windowVisible = currentWindow is not null && currentWindow.Width > 0 && currentWindow.Height > 0;
		_navigationState.Text = $"Navigation={_navigationSequence}; Page={(showingIndex ? "Index" : "Help")}; WindowVisible={windowVisible}; HelpSelected=True; Identity={_identityToken}";

		if (_awaitingFirstBack && !showingIndex)
		{
			_awaitingFirstBack = false;
			_historyState.Text = $"First back completed; CanGoBack={_helpWebView.CanGoBack}; Navigation={_navigationSequence}; Identity={_identityToken}";
		}
	}

	async void OnBackClicked(object sender, EventArgs e)
	{
		bool canGoBack = _helpWebView.CanGoBack;

		if (_backRequests > 0)
			_repeatedBackState.Text = $"Repeated back observed CanGoBack={canGoBack}";

		if (canGoBack)
		{
			_backRequests++;
			_awaitingFirstBack = _backRequests == 1;
			_helpWebView.GoBack();
		}
		else
		{
			await Shell.Current.Navigation.PopAsync();
		}
	}
}
#endif
