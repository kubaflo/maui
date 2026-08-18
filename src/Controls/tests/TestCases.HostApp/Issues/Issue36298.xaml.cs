namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36298, "ContentPresenter throws when retained RefreshView content is reattached", PlatformAffected.WinRT)]
public partial class Issue36298 : ContentPage
{
	readonly ContentView _viewOne;
	readonly ContentView _viewTwo;
	object _initialViewOnePlatformView = new();
	bool _viewOneWasLoaded;

	public Issue36298()
	{
		InitializeComponent();

		_viewOne = CreateView("View 1", "ViewOneMarker");
		_viewTwo = CreateView("View 2", "ViewTwoMarker");

		_viewOne.Loaded += OnViewOneLoaded;
		_viewTwo.Loaded += OnViewTwoLoaded;
		ContentHost.Content = _viewOne;
	}

#if WINDOWS
	protected override void OnAppearing()
	{
		base.OnAppearing();
		Microsoft.UI.Xaml.Application.Current.UnhandledException += OnUnhandledException;
	}

	protected override void OnDisappearing()
	{
		Microsoft.UI.Xaml.Application.Current.UnhandledException -= OnUnhandledException;
		base.OnDisappearing();
	}

	void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
	{
		if (args.Exception is not ArgumentException exception ||
			!exception.Message.Contains("PointerEventRouter", StringComparison.Ordinal) ||
			!exception.Message.Contains("already has an owner", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		args.Handled = true;
		ReattachmentStatus.Text = $"View 1 reattachment threw ArgumentException: {exception.Message}";
	}
#endif

	static ContentView CreateView(string text, string automationId)
	{
		return new ContentView
		{
			Content = new RefreshView
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Children =
						{
							new Label
							{
								AutomationId = automationId,
								FontSize = 24,
								Text = text
							},
							new Label { Text = "RefreshView with ScrollView content" }
						}
					}
				}
			}
		};
	}

	void OnViewOneLoaded(object sender, EventArgs e)
	{
		if (!ReferenceEquals(ContentHost.Content, _viewOne) || _viewOne.Handler is null)
			return;

		if (!_viewOneWasLoaded)
		{
			_viewOneWasLoaded = true;
			_initialViewOnePlatformView = _viewOne.Handler.PlatformView;
			ReattachmentStatus.Text = "View 1 attached";
			return;
		}

		ReattachmentStatus.Text = ReferenceEquals(_initialViewOnePlatformView, _viewOne.Handler.PlatformView)
			? "View 1 reattached"
			: "View 1 native hierarchy was replaced";
	}

	void OnViewTwoLoaded(object sender, EventArgs e)
	{
		if (ReferenceEquals(ContentHost.Content, _viewTwo) && _viewTwo.Handler is not null)
			ReattachmentStatus.Text = "View 2 attached";
	}

	void OnSwitchToViewTwoClicked(object sender, EventArgs e)
	{
		ContentHost.Content = _viewTwo;
	}

	void OnSwitchBackToViewOneClicked(object sender, EventArgs e)
	{
		try
		{
			ContentHost.Content = _viewOne;
		}
		catch (ArgumentException exception)
		{
			ReattachmentStatus.Text = $"View 1 assignment threw ArgumentException: {exception.Message}";
		}
	}

}
