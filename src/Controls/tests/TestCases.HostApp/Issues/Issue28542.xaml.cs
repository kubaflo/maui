#if ANDROID
using AndroidX.RecyclerView.Widget;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28542, "CollectionView scrollbar has incorrect sizing for variable-height items", PlatformAffected.Android)]
public partial class Issue28542 : ContentPage
{
	readonly Issue28542Item[] items;
	int captureSequence = -1;

	public Issue28542()
	{
		InitializeComponent();

		items = Enumerable.Range(1, 16)
			.Select(index => new Issue28542Item(
				index <= 8 ? $"Short item {index}" : $"Tall item {index}",
				index <= 8 ? 56 : 180))
			.ToArray();

		VariableHeightCollection.ItemsSource = items;
	}

	void OnCaptureMetricsClicked(object sender, EventArgs e)
	{
#if ANDROID
		if (VariableHeightCollection.Handler?.PlatformView is not RecyclerView recyclerView)
		{
			ResultLabel.Text = "RecyclerView unavailable";
			return;
		}

		var adapter = recyclerView.GetAdapter();
		var layoutManager = recyclerView.GetLayoutManager();
		if (adapter is null || layoutManager is not LinearLayoutManager linearLayoutManager)
		{
			ResultLabel.Text = "RecyclerView metrics unavailable";
			return;
		}

		captureSequence++;
		var extent = recyclerView.ComputeVerticalScrollExtent();
		var range = recyclerView.ComputeVerticalScrollRange();
		var offset = recyclerView.ComputeVerticalScrollOffset();
		var thumb = extent > 0 && range > 0
			? (int)Math.Round((double)extent * extent / range)
			: 0;
		var identities = string.Join(",", items.Select(item => item.Text));
		var density = DeviceDisplay.Current.MainDisplayInfo.Density;

		ResultLabel.Text = FormattableString.Invariant(
			$"sequence={captureSequence};extent={extent};range={range};offset={offset};first={linearLayoutManager.FindFirstVisibleItemPosition()};last={linearLayoutManager.FindLastVisibleItemPosition()};density={density};nativeCount={adapter.ItemCount};managedCount={items.Length};attached={(recyclerView.IsAttachedToWindow ? 1 : 0)};shortHeight=56;tallHeight=180;thumb={thumb};items={identities};");
#else
		ResultLabel.Text = "Android metrics unavailable";
#endif
	}
}

public sealed record Issue28542Item(string Text, double Height);
