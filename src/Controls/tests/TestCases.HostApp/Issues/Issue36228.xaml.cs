using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36228, "Repeated button taps open the destination page multiple times", PlatformAffected.Android)]
public class Issue36228 : NavigationPage
{
	public Issue36228() : base(new Issue36228RootPage())
	{
	}
}

public partial class Issue36228RootPage : ContentPage
{
	int _tapCount;

	public Issue36228RootPage()
	{
		InitializeComponent();
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		_tapCount++;

		if (_tapCount != 3)
			return;

		var navigations = new[]
		{
			Navigation.PushAsync(CreateDestinationPage()),
			Navigation.PushAsync(CreateDestinationPage()),
			Navigation.PushAsync(CreateDestinationPage())
		};

		await Task.WhenAll(navigations);
	}

	void OnResetClicked(object sender, EventArgs e)
	{
		_tapCount = 0;
		ResultLabel.Text = "-1";
	}

	ContentPage CreateDestinationPage()
	{
		var checkItem = new ToolbarItem
		{
			Text = "Check navigation stack",
			AutomationId = "Issue36228CheckNavigationStack"
		};
		checkItem.Clicked += OnCheckNavigationStack;

		var page = new ContentPage
		{
			Title = "New Page1",
			Content = new Label
			{
				Text = "Welcome to New Page 1",
				AutomationId = "Issue36228Destination",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};
		page.ToolbarItems.Add(checkItem);
		return page;
	}

	async void OnCheckNavigationStack(object sender, EventArgs e)
	{
		var destinationCount = Navigation.NavigationStack.Count - 1;
		ResultLabel.Text = $"Destination count: {destinationCount.ToString(CultureInfo.InvariantCulture)}";
		await Navigation.PopToRootAsync();
	}
}
