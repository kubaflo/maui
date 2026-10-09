namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28893, "[iOS] CarouselView with bindable gradient Border crashes app", PlatformAffected.iOS)]
public partial class Issue28893 : ContentPage
{
	public Issue28893()
	{
		InitializeComponent();
	}

	void OnUpdateItemsClicked(object sender, EventArgs e)
	{
		GradientCarousel.ItemsSource = new[]
		{
			new GradientItem("Red to orange", Colors.Red, Colors.Orange),
			new GradientItem("Blue to purple", Colors.Blue, Colors.Purple),
			new GradientItem("Green to yellow", Colors.Green, Colors.Yellow),
		};
	}

	sealed class GradientItem
	{
		public GradientItem(string title, Color startColor, Color endColor)
		{
			Title = title;
			StartColor = startColor;
			EndColor = endColor;
		}

		public string Title { get; }

		public Color StartColor { get; }

		public Color EndColor { get; }
	}
}
