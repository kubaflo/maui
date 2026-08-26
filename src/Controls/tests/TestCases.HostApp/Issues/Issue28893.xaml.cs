using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28893, "[iOS] CarouselView with Bindable Gradient Border crash app", PlatformAffected.iOS)]
public partial class Issue28893 : ContentPage
{
	public ObservableCollection<Issue28893CarouselItem> Items { get; } = [];

	public Issue28893()
	{
		InitializeComponent();
		BindingContext = this;
	}

	void OnRefreshClicked(object sender, EventArgs e)
	{
		Items.Add(new("First", "#FF006E", "#3A86FF"));
		Items.Add(new("Second", "#8338EC", "#FFBE0B"));
		Items.Add(new("Third", "#FB5607", "#06D6A0"));
		Items.Add(new("Fourth", "#3A86FF", "#FF006E"));
		ResultLabel.Text = "Added four items";
	}
}

public sealed record Issue28893CarouselItem(string ItemTitle, string StartColor, string EndColor);
