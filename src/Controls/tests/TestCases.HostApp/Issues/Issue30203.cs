namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30203, "[Windows] Unable to adjust the window background color visible when navigating", PlatformAffected.UWP)]
public partial class Issue30203 : NavigationPage
{
	static readonly Color AppBackground = Color.FromArgb("#FFF4E6");

	Label _pushCompletionLabel = null!;
	readonly Label _popCompletionLabel;
	int _pushCompletion = -1;
	int _popCompletion = -1;

	public Issue30203() : base(new ContentPage())
	{
		Title = "Issue 30203";

		_popCompletionLabel = new Label
		{
			AutomationId = "PopCompletion",
			FontSize = 10,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "-1"
		};
		var pageA = (ContentPage)CurrentPage;
		pageA.Title = "Page A";
		pageA.BackgroundColor = AppBackground;
		pageA.Content = CreatePageAContent();
	}

	Grid CreatePageAContent()
	{
		var navigateButton = new Button
		{
			AutomationId = "NavigateButton",
			Text = "Navigate to Page B"
		};
		navigateButton.Clicked += OnNavigateClicked;

		return CreatePageLayout(
			new Label
			{
				AutomationId = "PageAMarker",
				FontAttributes = FontAttributes.Bold,
				FontSize = 28,
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Page A - app background"
			},
			new Label
			{
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Both pages use the same light app background."
			},
			_popCompletionLabel,
			navigateButton);
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		var frameBackgroundLabel = CreateDiagnosticLabel("FrameBackground", "-1");
		var returnButton = new Button
		{
			AutomationId = "ReturnButton",
			Text = "Return to Page A"
		};
		returnButton.Clicked += OnReturnClicked;
		_pushCompletionLabel = CreateDiagnosticLabel("PushCompletion", "-1");

		var destination = new ContentPage
		{
			Title = "Page B",
			BackgroundColor = AppBackground,
			Content = CreatePageLayout(
				new Label
				{
					AutomationId = "PageBMarker",
					FontAttributes = FontAttributes.Bold,
					FontSize = 28,
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "Page B - app background"
				},
				new Label
				{
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "The destination keeps the same light app background."
				},
				_pushCompletionLabel,
				frameBackgroundLabel,
				returnButton)
		};

		var completion = _pushCompletion + 1;
		await PushAsync(destination, true);
		_pushCompletion = completion;
		_pushCompletionLabel.Text = $"Completed:{completion}";
#if WINDOWS
		frameBackgroundLabel.Text = $"Expected:#FFF4E6;Actual:{GetFrameBackground()}";
#endif
	}

	async void OnReturnClicked(object sender, EventArgs e)
	{
		var completion = _popCompletion + 1;
		await PopAsync(true);
		_popCompletion = completion;
		_popCompletionLabel.Text = $"Completed:{completion}";
	}

	static Label CreateDiagnosticLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			FontSize = 10,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = text
		};

	static Grid CreatePageLayout(params View[] children)
	{
		var stack = new VerticalStackLayout
		{
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Spacing = 18
		};

		foreach (var child in children)
			stack.Children.Add(child);

		return new Grid
		{
			Padding = 32,
			Children = { stack }
		};
	}
}
