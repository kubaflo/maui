namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.UWP)]
public class Issue35301 : ContentPage
{
	public Issue35301()
	{
		var callbackCount = -1;
		var statusLabel = new Label
		{
			AutomationId = "Issue35301Status",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18,
			Text = "InitialCallbacks=-1;Callbacks=-1;Selected=<none>;ChromeStyle=<not-inspected>"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "Issue35301Collection",
			SelectionMode = SelectionMode.Single,
			ItemsSource = new[]
			{
				new Issue35301Item("Apple", "Issue35301Apple"),
				new Issue35301Item("Banana", "Issue35301Banana"),
				new Issue35301Item("Cherry", "Issue35301Cherry")
			},
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 20,
					Padding = 12
				};
				label.SetBinding(Label.TextProperty, nameof(Issue35301Item.Name));
				label.SetBinding(Label.AutomationIdProperty, nameof(Issue35301Item.ItemAutomationId));
				return label;
			})
		};

		collectionView.SelectionChanged += (_, args) =>
		{
			callbackCount++;
			var selectedName = args.CurrentSelection.FirstOrDefault() is Issue35301Item item
				? item.Name
				: "<none>";
			statusLabel.Text = $"InitialCallbacks=-1;Callbacks={callbackCount};Selected={selectedName};ChromeStyle={GetChromeStyleState(collectionView)}";
		};

		Loaded += (_, _) =>
		{
			callbackCount = 0;
			statusLabel.Text = "InitialCallbacks=-1;Callbacks=0;Selected=<none>;ChromeStyle=<not-inspected>";
		};

		var layout = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		layout.Add(new Label
		{
			FontSize = 18,
			Text = "Select Apple in the default single-selection CollectionView."
		});
		layout.Add(collectionView, 0, 1);
		layout.Add(statusLabel, 0, 2);
		Content = layout;
	}

	static string GetChromeStyleState(CollectionView collectionView)
	{
#if WINDOWS
		if (collectionView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListViewBase listView &&
			listView.SelectedItem is not null &&
			listView.ContainerFromItem(listView.SelectedItem) is Microsoft.UI.Xaml.Controls.ListViewItem container &&
			Microsoft.UI.Xaml.Application.Current.Resources["DefaultListViewItemStyle"] is Microsoft.UI.Xaml.Style defaultStyle)
		{
			return ReferenceEquals(container.Style?.BasedOn, defaultStyle) ? "Present" : "Absent";
		}
#endif
		return "InspectionFailed";
	}

	sealed record Issue35301Item(string Name, string ItemAutomationId);
}

