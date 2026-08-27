#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37657, "Shell back handling is not called on the root page of a Shell tab", PlatformAffected.Android)]
public class Issue37657 : Shell
{
	const string AwaitingBack = "Awaiting Android Back";
	const string BackCompleted = "Android Back callback completed";
	const string WindowStopped = "Window stopped before callback";

	static int s_backButtonPressCount;
	static string s_lifecycleToken = AwaitingBack;
	static bool s_secondRootArmed;
	static bool s_reopeningAfterStop;

	public Issue37657()
	{
		if (s_reopeningAfterStop)
		{
			s_reopeningAfterStop = false;
		}
		else
		{
			s_backButtonPressCount = 0;
			s_lifecycleToken = AwaitingBack;
			s_secondRootArmed = false;
		}

		Loaded += OnLoaded;

		var firstTab = new Tab
		{
			Title = "First",
			AutomationId = "FirstTab"
		};
		firstTab.Items.Add(new ShellContent
		{
			Title = "First",
			Route = "FirstRoot",
			ContentTemplate = new DataTemplate(() => new RootPage("First"))
		});

		var secondTab = new Tab
		{
			Title = "Second",
			AutomationId = "SecondTab"
		};
		secondTab.Items.Add(new ShellContent
		{
			Title = "Second",
			Route = "SecondRoot",
			ContentTemplate = new DataTemplate(() => new RootPage("Second"))
		});

		var tabBar = new TabBar();
		tabBar.Items.Add(firstTab);
		tabBar.Items.Add(secondTab);
		Items.Add(tabBar);
	}

	void OnLoaded(object sender, EventArgs e)
	{
		if (Window is Window issueWindow)
		{
			issueWindow.Stopped -= OnWindowStopped;
			issueWindow.Stopped += OnWindowStopped;
		}
	}

	void OnWindowStopped(object sender, EventArgs e)
	{
		if (sender is Window stoppedWindow)
			stoppedWindow.Stopped -= OnWindowStopped;

		if (!s_secondRootArmed || s_backButtonPressCount != 0)
			return;

		s_secondRootArmed = false;
		s_lifecycleToken = WindowStopped;
		s_reopeningAfterStop = true;

		if (Application.Current is Application application)
			application.OpenWindow(new Window(new Issue37657()));
	}

	protected override bool OnBackButtonPressed()
	{
		s_backButtonPressCount++;
		s_lifecycleToken = BackCompleted;

		if (CurrentPage is RootPage rootPage)
			rootPage.UpdateStatus();

		return true;
	}

	sealed class RootPage : ContentPage
	{
		readonly string _rootName;
		readonly Label _callbackCountLabel;
		readonly Label _lifecycleTokenLabel;

		public RootPage(string rootName)
		{
			_rootName = rootName;
			_callbackCountLabel = new Label
			{
				AutomationId = "BackCallbackCount",
				FontSize = 20
			};
			_lifecycleTokenLabel = new Label
			{
				AutomationId = "LifecycleToken",
				FontSize = 20,
				FontAttributes = FontAttributes.Bold
			};

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						Text = "Issue 37657: Shell back handling",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "This is the root page of the selected Shell tab. Select Second, then press the Android device Back button.",
						FontSize = 16
					},
					new Label
					{
						AutomationId = "CurrentRoot",
						Text = $"Current root: {_rootName}",
						FontSize = 20
					},
					_callbackCountLabel,
					_lifecycleTokenLabel
				}
			};

			UpdateStatus();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			s_secondRootArmed = _rootName == "Second";
			UpdateStatus();
		}

		internal void UpdateStatus()
		{
			_callbackCountLabel.Text = $"Back callback count: {s_backButtonPressCount}";
			_lifecycleTokenLabel.Text = s_lifecycleToken;
		}
	}
}
#endif

