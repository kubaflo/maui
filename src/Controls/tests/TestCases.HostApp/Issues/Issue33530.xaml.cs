using Microsoft.Maui;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "Border with Rotation and HorizontalOptions.Start is positioned incorrectly on initial load", PlatformAffected.Android)]
public partial class Issue33530 : ContentPage
{
	int _initialLayoutGeneration = -1;

	public Issue33530()
	{
		InitializeComponent();
	}

	async void OnOpenInitialClicked(object sender, EventArgs e)
	{
		OpenInitialButton.IsEnabled = false;
		var template = (DataTemplate)Resources["InitialBorderTemplate"];
		var modalPage = (ContentPage)template.CreateContent();

		await Navigation.PushModalAsync(modalPage, false);
	}

	void OnInitialBorderSizeChanged(object sender, EventArgs e)
	{
		var border = (Border)sender;
		if (_initialLayoutGeneration >= 0 ||
			border.Handler is null ||
			border.Width <= 0 ||
			border.Height <= 0)
		{
			return;
		}

		_initialLayoutGeneration = 0;
		UpdateStatus(border, "UNCHECKED");
	}

	void OnCheckInitialLayoutClicked(object sender, EventArgs e)
	{
		var button = (Button)sender;
		var content = (VerticalStackLayout)button.Parent;
		var border = (Border)content.Parent;
		var modalPage = (ContentPage)border.Parent;
		var expectedX = border.Height - border.Padding.Left;
		var expectedY = modalPage.Height / 2;
		var elements = VisualTreeElementExtensions.GetVisualTreeElements(
			modalPage.Window,
			expectedX,
			expectedY);
		var edge = "MISSING";
		foreach (var element in elements)
		{
			if (element == border)
			{
				edge = "ALIGNED";
				break;
			}
		}

		UpdateStatus(border, edge);
	}

	void UpdateStatus(Border border, string edge)
	{
		var content = (VerticalStackLayout)border.Content;
		var status = (Label)content.Children[2];
		status.Text = $"INITIAL;generation={_initialLayoutGeneration};edge={edge}";
	}
}
