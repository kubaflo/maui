#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 17877, "TabbedPage does not notify when the current tab is reselected", PlatformAffected.Android)]
public class Issue17877 : TestNavigationPage
{
	const string PageChangeStatusPrefix = "CurrentPageChanged count:";
	const string ReselectionStatusPrefix = "CurrentPageReselected count:";

	TabbedPage _issueTabs = null!;
	Label _pageChangeStatus = null!;
	Label _reselectionStatus = null!;
	int _pageChangeCount;
	int _reselectionCount;
	bool _isReady;

	protected override void Init()
	{
		_pageChangeCount = -1;
		_reselectionCount = -1;

		_pageChangeStatus = CreateStatusLabel(
			"Issue17877PageChangeStatus",
			PageChangeStatusPrefix,
			_pageChangeCount);
		_reselectionStatus = CreateStatusLabel(
			"Issue17877ReselectionStatus",
			ReselectionStatusPrefix,
			_reselectionCount);

		var readyStatus = new Label
		{
			AutomationId = "Issue17877ReadyStatus",
			Text = "Bottom tabs loaded: 0"
		};

		_issueTabs = new TabbedPage
		{
			Children =
			{
				new ContentPage
				{
					Title = "Tab 1",
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label
							{
								AutomationId = "Issue17877TabOneContent",
								Text = "Tab 1 content"
							},
							readyStatus,
							_pageChangeStatus,
							_reselectionStatus
						}
					}
				},
				new ContentPage
				{
					Title = "Tab 2",
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Children =
						{
							new Label
							{
								AutomationId = "Issue17877TabTwoContent",
								Text = "Tab 2 content"
							}
						}
					}
				}
			}
		};

		Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(
			_issueTabs,
			Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);

		_issueTabs.CurrentPageChanged += OnCurrentPageChanged;

		var reselectionEvent = typeof(TabbedPage).GetEvent("CurrentPageReselected");
		if (reselectionEvent?.EventHandlerType is Type handlerType)
		{
			var handler = Delegate.CreateDelegate(handlerType, this, nameof(OnCurrentPageReselected));
			reselectionEvent.AddEventHandler(_issueTabs, handler);
		}

		_issueTabs.Loaded += OnTabsLoaded;

		var issuePage = new ContentPage
		{
			Content = new Label { Text = "Loading bottom tabs" }
		};
		issuePage.Loaded += OnIssuePageLoaded;
		PushAsync(issuePage);

		void OnTabsLoaded(object sender, EventArgs e)
		{
			_issueTabs.Loaded -= OnTabsLoaded;
			_pageChangeCount = 0;
			_reselectionCount = 0;
			UpdateStatus(_pageChangeStatus, PageChangeStatusPrefix, _pageChangeCount);
			UpdateStatus(_reselectionStatus, ReselectionStatusPrefix, _reselectionCount);
			_isReady = true;
			readyStatus.Text = "Bottom tabs loaded: 1";
		}
	}

	async void OnIssuePageLoaded(object sender, EventArgs e)
	{
		if (sender is not ContentPage issuePage)
			throw new InvalidOperationException("The issue page must initiate the reported navigation.");

		issuePage.Loaded -= OnIssuePageLoaded;
		await issuePage.Navigation.PushAsync(_issueTabs);
	}

	void OnCurrentPageChanged(object sender, EventArgs e)
	{
		if (!_isReady)
			return;

		_pageChangeCount++;
		UpdateStatus(_pageChangeStatus, PageChangeStatusPrefix, _pageChangeCount);
	}

	void OnCurrentPageReselected(object sender, EventArgs e)
	{
		if (!_isReady)
			return;

		_reselectionCount++;
		UpdateStatus(_reselectionStatus, ReselectionStatusPrefix, _reselectionCount);
	}

	static void UpdateStatus(Label label, string prefix, int value)
	{
		label.Text = $"{prefix} {value}";
	}

	static Label CreateStatusLabel(string automationId, string prefix, int value)
	{
		return new Label
		{
			AutomationId = automationId,
			Text = $"{prefix} {value}"
		};
	}
}
#endif

