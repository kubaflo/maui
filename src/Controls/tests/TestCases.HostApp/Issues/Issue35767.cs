#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35767, "SearchHandler.ShowsResults does not work correctly", PlatformAffected.UWP)]
public class Issue35767 : TestShell
{
	protected override void Init()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		var searchHandler = new Issue35767SearchHandler
		{
			AutomationId = "Issue35767SearchHandler",
			Placeholder = "Search items",
			ShowsResults = true,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					Padding = 12,
					FontSize = 18
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		var transitionCount = -1;
		var transitionStatus = new Label
		{
			AutomationId = "Issue35767TransitionStatus",
			Text = "Count=-1; ShowsResults=unset"
		};

		searchHandler.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(SearchHandler.ShowsResults))
			{
				transitionCount = transitionCount == -1 ? 1 : transitionCount + 1;
				transitionStatus.Text = $"Count={transitionCount}; ShowsResults={searchHandler.ShowsResults}";
			}
		};

		var disableResultsButton = new Button
		{
			AutomationId = "Issue35767DisableResults",
			Text = "ShowsResults = False"
		};
		disableResultsButton.Clicked += (_, _) => searchHandler.ShowsResults = false;

		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "SearchHandler ShowsResults runtime update" },
					transitionStatus,
					disableResultsButton
				}
			}
		};

		Shell.SetSearchHandler(page, searchHandler);
		Items.Add(new ShellContent
		{
			Title = "SearchHandler ShowsResults",
			Content = page
		});
	}

	sealed class Issue35767SearchHandler : SearchHandler
	{
		readonly string[] _items = ["alpha result", "beta result"];

		protected override void OnQueryChanged(string oldValue, string newValue)
		{
			base.OnQueryChanged(oldValue, newValue);

			ItemsSource = string.IsNullOrWhiteSpace(newValue)
				? null
				: _items.Where(item => item.Contains(newValue, StringComparison.OrdinalIgnoreCase)).ToList();
		}
	}
}
#endif

