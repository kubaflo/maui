namespace Controls.TestCases.HostApp.Issues;

[Issue(IssueTracker.Github, 30776, "Project and task data reloads after returning from detail", PlatformAffected.iOS)]
public class Issue30776 : NavigationPage
{
	public Issue30776() : base(new Issue30776HomePage()) { }
}

class Issue30776HomePage : ContentPage
{
	readonly Label _loadCountLabel;
	readonly Label _transitionLabel;
	int _projectAndTaskDataLoadCount;

	public Issue30776HomePage()
	{
		Title = "Developer balance";

		_loadCountLabel = new Label
		{
			Text = "Project and task data loads: 0",
			AutomationId = "Issue30776LoadCountLabel"
		};

		_transitionLabel = new Label
		{
			Text = "transition:-1",
			AutomationId = "Issue30776TransitionLabel"
		};

		var projectBalanceButton = new Button
		{
			Text = "Project Balance",
			AutomationId = "Issue30776ProjectBalanceButton"
		};
		projectBalanceButton.Clicked += OnProjectBalanceClicked;

		var scrollView = new ScrollView
		{
			AutomationId = "Issue30776ScrollView",
			Content = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label { Text = "Created projects and tasks" },
					new Button { Text = "Planning task" },
					new Button { Text = "Review task" },
					new BoxView { HeightRequest = 500 },
					projectBalanceButton,
					new Button { Text = "Documentation task" }
				}
			}
		};

		var rootGrid = new Grid
		{
			Padding = 20,
			RowSpacing = 12,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		rootGrid.Add(new Label
		{
			Text = "Home Screen",
			FontSize = 24,
			AutomationId = "Issue30776HomeHeading"
		});
		rootGrid.Add(new VerticalStackLayout
		{
			Spacing = 6,
			Children =
			{
				_loadCountLabel,
				_transitionLabel
			}
		}, 0, 1);
		rootGrid.Add(scrollView, 0, 2);

		Content = rootGrid;
		Loaded += OnHomeLoaded;
	}

	void OnHomeLoaded(object sender, EventArgs e)
	{
		_projectAndTaskDataLoadCount++;
		_loadCountLabel.Text = $"Project and task data loads: {_projectAndTaskDataLoadCount}";
	}

	async void OnProjectBalanceClicked(object sender, EventArgs e)
	{
		var closeButton = new Button
		{
			Text = "Close project",
			AutomationId = "Issue30776CloseProjectButton"
		};
		closeButton.Clicked += OnCloseProjectClicked;

		var detailPage = new ContentPage
		{
			Title = "Project Balance",
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Project Balance",
						FontSize = 24,
						AutomationId = "Issue30776ProjectDetailHeading"
					},
					new Label { Text = "Project details and actions" },
					closeButton
				}
			}
		};

		await Navigation.PushAsync(detailPage);
	}

	async void OnCloseProjectClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
		_transitionLabel.Text = "transition:return-completed";
	}
}

