using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37263, "ScrollView Container safe area is horizontally misaligned in landscape", PlatformAffected.iOS)]
public partial class Issue37263 : ContentPage
{
	bool _hasLayoutSnapshot;
	double _snapshotWidth;
	double _snapshotHeight;
	SafeAreaEdges _snapshotEdges;
	int _layoutGeneration = -1;

	public Issue37263()
	{
		InitializeComponent();
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);

		var edges = TestScrollView.SafeAreaEdges;
		if (_hasLayoutSnapshot &&
			_snapshotWidth == width &&
			_snapshotHeight == height &&
			_snapshotEdges == edges)
		{
			return;
		}

		_hasLayoutSnapshot = true;
		_snapshotWidth = width;
		_snapshotHeight = height;
		_snapshotEdges = edges;
		var generation = ++_layoutGeneration;
		var mode = edges == SafeAreaEdges.Default ? "Default" : "Container";

		Dispatcher.Dispatch(() => UpdateLayoutStatus(generation, mode));
	}

	void OnContainerClicked(object sender, EventArgs e)
	{
		TestScrollView.SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
		ModeLabel.Text = "SafeAreaEdges: Container";
		UpdateLayoutStatus(++_layoutGeneration, "Container");
	}

	void UpdateLayoutStatus(int generation, string mode)
	{
		var insets = this.On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>().SafeAreaInsets();
		LayoutStatusLabel.Text = string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			"Generation={0};Mode={1};InsetLeft={2:F2};InsetRight={3:F2}",
			generation,
			mode,
			insets.Left,
			insets.Right);
	}
}
