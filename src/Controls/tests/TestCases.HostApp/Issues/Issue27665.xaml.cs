namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding or showing elements from ScrollView.Scrolled on Android", PlatformAffected.Android)]
public partial class Issue27665 : ContentPage
{
	public Issue27665()
	{
		InitializeComponent();
		StartNativeObservation();
	}

	void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
	{
		RecordNativeScroll();
		bool shouldBeVisible = e.ScrollY <= 0;
		EntryTest.IsVisible = shouldBeVisible;
		ImageTest.IsVisible = shouldBeVisible;
	}

	protected override void OnDisappearing()
	{
		StopNativeObservation();
		base.OnDisappearing();
	}

	partial void StartNativeObservation();
	partial void RecordNativeScroll();
	partial void StopNativeObservation();
}
