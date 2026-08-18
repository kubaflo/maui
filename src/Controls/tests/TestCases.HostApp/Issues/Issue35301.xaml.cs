namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35301, "Windows CollectionView applies WinUI styling by default", PlatformAffected.UWP)]
public partial class Issue35301 : ContentPage
{
	public Issue35301()
	{
		InitializeComponent();
		FruitCollection.ItemsSource = new[] { "Apple", "Banana", "Cherry" };
	}

	void OnCollectionLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		if (FruitCollection.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ListView listView)
			return;

		var theme = Application.Current?.RequestedTheme.ToString() ?? "<none>";
		var windowSize = Window is null
			? "<none>"
			: FormattableString.Invariant($"{Window.Width}x{Window.Height}");
		Console.WriteLine($"Issue35301 environment: Theme={theme}; Window={windowSize}");

		listView.ContainerContentChanging += OnContainerContentChanging;
		UpdateReadyState(listView);
#endif
	}

	void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
#if WINDOWS
		if (e.CurrentSelection.Count != 1 ||
			e.CurrentSelection[0] is not string selected ||
			selected != "Apple" ||
			FruitCollection.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ListView listView)
		{
			return;
		}

		UpdateSelectedState(listView);
#endif
	}

#if WINDOWS
	void UpdateSelectedState(Microsoft.UI.Xaml.Controls.ListView listView)
	{
		var appleContainer = listView.ContainerFromIndex(0) as Microsoft.UI.Xaml.Controls.ListViewItem;
		if (listView.Items.Count != 3 ||
			listView.Items[0] is not Microsoft.Maui.Controls.Platform.ItemTemplateContext appleContext ||
			appleContext.Item is not string apple ||
			apple != "Apple" ||
			appleContainer is null)
		{
			SelectionStateLabel.Text = "Selected: Apple; native Apple container missing";
			return;
		}

		var cornerValue = GetCornerRadiusValue(listView);
		var indicatorValue = GetSelectionIndicatorValue(listView);
		NativeStateLabel.Text = $"Corner: {cornerValue}; Indicator: {indicatorValue}";
		SelectionStateLabel.Text = $"Selected: Apple; Callback: True; Native index: 0; IsSelected: {appleContainer.IsSelected}";
	}

	void OnContainerContentChanging(
		Microsoft.UI.Xaml.Controls.ListViewBase sender,
		Microsoft.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
	{
		UpdateReadyState((Microsoft.UI.Xaml.Controls.ListView)sender);
	}

	void UpdateReadyState(Microsoft.UI.Xaml.Controls.ListView listView)
	{
		if (listView.Items.Count != 3 ||
			listView.Items[0] is not Microsoft.Maui.Controls.Platform.ItemTemplateContext appleContext ||
			listView.Items[1] is not Microsoft.Maui.Controls.Platform.ItemTemplateContext bananaContext ||
			listView.Items[2] is not Microsoft.Maui.Controls.Platform.ItemTemplateContext cherryContext ||
			appleContext.Item is not string apple ||
			bananaContext.Item is not string banana ||
			cherryContext.Item is not string cherry ||
			apple != "Apple" ||
			banana != "Banana" ||
			cherry != "Cherry" ||
			listView.ContainerFromIndex(0) is not Microsoft.UI.Xaml.Controls.ListViewItem appleContainer ||
			listView.ContainerFromIndex(1) is null ||
			listView.ContainerFromIndex(2) is null)
		{
			return;
		}

		NativeStateLabel.Text = $"Native ready: Apple[0], Banana[1], Cherry[2]; Apple selected: {appleContainer.IsSelected}";
		listView.ContainerContentChanging -= OnContainerContentChanging;
	}

	static string GetCornerRadiusValue(Microsoft.UI.Xaml.Controls.ListView listView)
	{
		if (!listView.Resources.TryGetValue("ListViewItemCornerRadius", out var value))
			return "<missing>";

		if (value is not Microsoft.UI.Xaml.CornerRadius cornerRadius)
			return value?.ToString() ?? "<null>";

		return FormattableString.Invariant(
			$"CornerRadius({cornerRadius.TopLeft},{cornerRadius.TopRight},{cornerRadius.BottomRight},{cornerRadius.BottomLeft})");
	}

	static string GetSelectionIndicatorValue(Microsoft.UI.Xaml.Controls.ListView listView)
	{
		if (!listView.Resources.TryGetValue("ListViewItemSelectionIndicatorVisualEnabled", out var value))
			return "<missing>";

		return value?.ToString() ?? "<null>";
	}
#endif
}
