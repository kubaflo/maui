using System.Collections.ObjectModel;

#if ANDROID
using AndroidX.RecyclerView.Widget;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28542, "CollectionView scrollbar sizing with variable-height items", PlatformAffected.Android)]
public partial class Issue28542 : ContentPage
{
	const int ShortHeight = 70;
	const int TallHeight = 260;

#if ANDROID
	bool _targetReady;
	bool _dragStarted;
	int _scrollCallbackCount;
	int _lastVisibleItemIndex = -1;
	double _density;
#endif

	public ObservableCollection<RowItem> CalibrationRows { get; } = new();
	public ObservableCollection<RowItem> Rows { get; } = new();

	public Issue28542()
	{
		InitializeComponent();
		BindingContext = this;

		for (var index = 1; index <= 14; index++)
			CalibrationRows.Add(new RowItem($"Calibration row {index}", ShortHeight, Colors.LightBlue));

		for (var index = 1; index <= 8; index++)
			Rows.Add(new RowItem($"Short row {index}", ShortHeight, Colors.LightBlue));

		for (var index = 9; index <= 14; index++)
			Rows.Add(new RowItem($"Tall row {index}", TallHeight, Colors.LightGoldenrodYellow));

#if ANDROID
		CalibrationCollection.HandlerChanged += OnCalibrationHandlerChanged;
		VariableHeightCollection.HandlerChanged += OnTargetHandlerChanged;
#endif
	}

#if ANDROID
	void OnCalibrationHandlerChanged(object sender, EventArgs e)
	{
		if (CalibrationCollection.Handler?.PlatformView is not RecyclerView recyclerView)
			return;

		CalibrationCollection.HandlerChanged -= OnCalibrationHandlerChanged;
		recyclerView.LayoutChange += OnCalibrationLayoutChanged;
		TryPrepareCalibration(recyclerView);
	}

	void OnCalibrationLayoutChanged(object sender, Android.Views.View.LayoutChangeEventArgs e)
	{
		if (sender is RecyclerView recyclerView)
			TryPrepareCalibration(recyclerView);
	}

	void TryPrepareCalibration(RecyclerView recyclerView)
	{
		if (recyclerView.ChildCount == 0)
			return;

		var firstChild = recyclerView.GetChildAt(0);
		if (firstChild is null || firstChild.Height <= 0)
			return;

		var extent = recyclerView.ComputeVerticalScrollExtent();
		var range = recyclerView.ComputeVerticalScrollRange();
		if (extent <= 0 || range <= 0)
			return;

		_density = (double)firstChild.Height / ShortHeight;
		var actualThumb = (double)extent * extent / range;
		var expectedThumb = (double)extent * extent / (CalibrationRows.Count * ShortHeight * _density);
		MeasurementStatus.Text = FormattableString.Invariant($"{actualThumb:F3}|{expectedThumb:F3}|{extent}|{range}|{_density:F6}|{firstChild.Height}");

		recyclerView.LayoutChange -= OnCalibrationLayoutChanged;
		CalibrationCollection.IsVisible = false;
		VariableHeightCollection.IsVisible = true;
	}

	void OnTargetHandlerChanged(object sender, EventArgs e)
	{
		if (VariableHeightCollection.Handler?.PlatformView is not RecyclerView recyclerView)
			return;

		VariableHeightCollection.HandlerChanged -= OnTargetHandlerChanged;
		recyclerView.LayoutChange += OnTargetLayoutChanged;
		TryPrepareTarget(recyclerView);
	}

	void OnTargetLayoutChanged(object sender, Android.Views.View.LayoutChangeEventArgs e)
	{
		if (sender is RecyclerView recyclerView)
			TryPrepareTarget(recyclerView);
	}

	void TryPrepareTarget(RecyclerView recyclerView)
	{
		if (_targetReady || RootLayout.Height <= RootLayout.Width || recyclerView.ChildCount == 0)
			return;

		var extent = recyclerView.ComputeVerticalScrollExtent();
		var range = recyclerView.ComputeVerticalScrollRange();
		if (extent <= 0 || range <= 0 || _density <= 0)
			return;

		_targetReady = true;
		_dragStarted = false;
		_scrollCallbackCount = 0;
		_lastVisibleItemIndex = -1;
		recyclerView.AddOnScrollListener(new ScrollStateListener(OnNativeScrollStateChanged));
		MeasurementStatus.Text += FormattableString.Invariant(
			$"#{extent}|{range}|{(double)extent * extent / range:F3}|{_scrollCallbackCount}|{_lastVisibleItemIndex}");
		ReadyStatus.Text = "Ready";
		recyclerView.LayoutChange -= OnTargetLayoutChanged;
	}
#endif

	void OnCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
#if ANDROID
		if (!_targetReady || !_dragStarted)
			return;

		_scrollCallbackCount++;
		_lastVisibleItemIndex = e.LastVisibleItemIndex;
#endif
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
#if ANDROID
		if (VariableHeightCollection.Handler?.PlatformView is not RecyclerView recyclerView)
			return;

		var extent = recyclerView.ComputeVerticalScrollExtent();
		var range = recyclerView.ComputeVerticalScrollRange();
		var actualThumb = range > 0 ? (double)extent * extent / range : 0;
		var expectedRange = ((8 * ShortHeight) + (6 * TallHeight)) * _density;
		var expectedThumb = expectedRange > 0 ? (double)extent * extent / expectedRange : 0;
		var tallRowIndex = -1;
		var tallRowHeight = -1;

		for (var index = 0; index < recyclerView.ChildCount; index++)
		{
			var child = recyclerView.GetChildAt(index);
			if (child is null || recyclerView.GetChildAdapterPosition(child) != 8)
				continue;

			tallRowIndex = 8;
			tallRowHeight = child.Height;
			break;
		}

		MeasurementStatus.Text += FormattableString.Invariant(
			$"#{actualThumb:F3}|{expectedThumb:F3}|{extent}|{range}|{expectedRange:F3}|{_density:F6}|{_scrollCallbackCount}|{_lastVisibleItemIndex}|{tallRowIndex}|{tallRowHeight}");
		ReadyStatus.Text = "Measurement complete";
#endif
	}

	void OnNativeScrollStateChanged(int state)
	{
#if ANDROID
		if (state == RecyclerView.ScrollStateDragging)
			_dragStarted = true;
		else if (state == RecyclerView.ScrollStateIdle && _dragStarted && _scrollCallbackCount > 0 && _lastVisibleItemIndex >= 8)
			ReadyStatus.Text = "Idle in tall rows";
#endif
	}

#if ANDROID
	sealed class ScrollStateListener : RecyclerView.OnScrollListener
	{
		readonly Action<int> _stateChanged;

		public ScrollStateListener(Action<int> stateChanged)
		{
			_stateChanged = stateChanged;
		}

		public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
		{
			base.OnScrollStateChanged(recyclerView, newState);
			_stateChanged(newState);
		}
	}
#endif

	public sealed record RowItem(string Text, double Height, Color Color);
}
