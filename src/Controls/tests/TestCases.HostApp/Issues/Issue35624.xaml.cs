namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing property is not applied", PlatformAffected.iOS)]
public partial class Issue35624 : ContentPage
{
	bool _shellInstalled;

	public Issue35624()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (_shellInstalled)
			return;

		_shellInstalled = true;
		InstallShellScenario();
	}

	void InstallShellScenario()
	{
		var scenarioWindow = Window;
		var resultLabel = new Label
		{
			AutomationId = "Issue35624Result",
			Text = "PENDING",
		};

		var searchHandler = new SearchHandler
		{
			AutomationId = "Issue35624Search",
			CharacterSpacing = 8,
			FontSize = 36,
			Placeholder = "Type MAUI",
			SearchBoxVisibility = SearchBoxVisibility.Collapsible,
		};

		searchHandler.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName != nameof(SearchHandler.Query) || searchHandler.Query != "MAUI")
				return;

			resultLabel.Text = "Query=MAUI; Callback=True; NativeText=PENDING; Kern=NaN; FullRange=False";

			resultLabel.Dispatcher.Dispatch(
				() => InspectNativeSearchField(scenarioWindow, resultLabel));
		};

		var contentPage = new ContentPage
		{
			Title = "SearchHandler CharacterSpacing",
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 18,
				Children =
				{
					new Label
					{
						AutomationId = "Issue35624Ready",
						FontSize = 20,
						Text = "Tap the search field and type MAUI",
					},
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						Text = "Expected: CharacterSpacing 8",
					},
					new Label
					{
						AutomationId = "Issue35624SpacedReference",
						CharacterSpacing = 8,
						FontSize = 36,
						Text = "MAUI",
					},
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						Text = "Default spacing",
					},
					new Label
					{
						AutomationId = "Issue35624DefaultReference",
						FontSize = 36,
						Text = "MAUI",
					},
					resultLabel,
				},
			},
		};

		Shell.SetSearchHandler(contentPage, searchHandler);

		var shell = new Shell
		{
			FlyoutBehavior = FlyoutBehavior.Disabled,
			Items =
			{
				new ShellContent
				{
					Content = contentPage,
					Title = "Issue 35624",
				},
			},
		};

		scenarioWindow.Page = shell;
	}

	static void InspectNativeSearchField(Window scenarioWindow, Label resultLabel)
	{
#if IOS
		var platformWindow = scenarioWindow.Handler?.PlatformView as UIKit.UIWindow;
		var textField = platformWindow is null ? null : FindSearchTextField(platformWindow);
		var nativeText = textField?.Text ?? "<missing>";
		var kern = double.NaN;
		var fullRange = false;
		var attributedText = textField?.AttributedText;

		if (attributedText is not null && attributedText.Length > 0)
		{
			var attribute = attributedText.GetAttribute(
				UIKit.UIStringAttributeKey.KerningAdjustment,
				0,
				out var range);

			if (attribute is Foundation.NSNumber number)
				kern = number.DoubleValue;

			fullRange = range.Location == 0 && range.Length == attributedText.Length;
		}

		resultLabel.Text = System.FormattableString.Invariant(
			$"Query=MAUI; Callback=True; NativeText={nativeText}; Kern={kern:0.###}; FullRange={fullRange}");
#endif
	}

#if IOS
	static UIKit.UITextField FindSearchTextField(UIKit.UIView view)
	{
		if (view is UIKit.UISearchBar searchBar)
			return searchBar.SearchTextField;

		foreach (var subview in view.Subviews)
		{
			var textField = FindSearchTextField(subview);
			if (textField is not null)
				return textField;
		}

		return null;
	}
#endif
}
