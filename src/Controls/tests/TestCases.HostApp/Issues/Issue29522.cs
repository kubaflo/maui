#if ANDROID
using ARect = Android.Graphics.Rect;
using AView = Android.Views.View;
using AViewTreeObserver = Android.Views.ViewTreeObserver;
#endif
using Microsoft.Maui.Layouts;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29522, "Scaled Editor is behind the keyboard on Android", PlatformAffected.Android)]
public class Issue29522 : ContentPage
{
	readonly Editor _trackedEditor;
	readonly Label _metricsLabel;

#if ANDROID
	readonly GlobalLayoutListener _globalLayoutListener;
	bool _isListening;
	int _sequence = -1;
	int _editorBottom = -1;
	int _visibleTop = -1;
	int _visibleBottom = -1;
	int _rootBottom = -1;
	int _editorWidth = -1;
	int _editorHeight = -1;
	bool _editorFocused;
	int _lastFocusedSequence = -1;
	int _lastFocusedEditorBottom = -1;
	int _lastFocusedVisibleBottom = -1;
	int _snapshotSequence = -1;
	int _snapshotEditorBottom = -1;
	int _snapshotVisibleBottom = -1;
#endif

	public Issue29522()
	{
		var scaledLayout = new AbsoluteLayout
		{
			Scale = 1.35
		};

		var editorStack = new VerticalStackLayout();
		for (int i = 1; i <= 9; i++)
		{
			editorStack.Children.Add(new Editor
			{
				Placeholder = $"Editor {i}"
			});
		}

		_trackedEditor = new Editor
		{
			AutomationId = "Issue29522Editor10",
			Placeholder = "Editor 10"
		};
		editorStack.Children.Add(_trackedEditor);
		editorStack.Children.Add(new Button { Text = "Button" });

		AbsoluteLayout.SetLayoutBounds(editorStack, new Rect(0.5, 0.5, 280, -1));
		AbsoluteLayout.SetLayoutFlags(editorStack, AbsoluteLayoutFlags.PositionProportional);
		scaledLayout.Children.Add(editorStack);

		_metricsLabel = new Label
		{
			AutomationId = "Issue29522Metrics",
			Text = "seq=-1;bottom=-1;top=-1;visible=-1;root=-1;focused=0;width=-1;height=-1;snapshotSeq=-1;snapshotBottom=-1;snapshotVisible=-1",
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			Padding = new Thickness(8, 4)
		};

		var checkOverlapButton = new Button
		{
			AutomationId = "Issue29522CheckOverlap",
			Text = "Check overlap"
		};
		checkOverlapButton.Clicked += OnCheckOverlapClicked;

		var measurementOverlay = new VerticalStackLayout
		{
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start,
			Spacing = 4,
			Margin = new Thickness(8),
			Children =
			{
				_metricsLabel,
				checkOverlapButton
			}
		};

		Content = new Grid
		{
			Children =
			{
				scaledLayout,
				measurementOverlay
			}
		};

#if ANDROID
		_globalLayoutListener = new GlobalLayoutListener(CaptureMetrics);
		Loaded += OnPageLoaded;
#endif
	}

	void OnCheckOverlapClicked(object sender, EventArgs e)
	{
#if ANDROID
		_snapshotSequence = _lastFocusedSequence;
		_snapshotEditorBottom = _lastFocusedEditorBottom;
		_snapshotVisibleBottom = _lastFocusedVisibleBottom;
		UpdateMetricsLabel();
#endif
	}

#if ANDROID
	void OnPageLoaded(object sender, EventArgs e)
	{
		if (_isListening || _trackedEditor.Handler?.PlatformView is not AView editorView)
			return;

		var observer = editorView.ViewTreeObserver;
		if (observer is null || !observer.IsAlive)
			return;

		observer.AddOnGlobalLayoutListener(_globalLayoutListener);
		_isListening = true;
		CaptureMetrics();
	}

	void CaptureMetrics()
	{
		if (_trackedEditor.Handler?.PlatformView is not AView editorView ||
			!editorView.IsAttachedToWindow ||
			editorView.Width <= 0 ||
			editorView.Height <= 0)
		{
			return;
		}

		var rootView = editorView.RootView;
		if (rootView is null)
			return;

		var location = new int[2];
		editorView.GetLocationOnScreen(location);

		var visibleWindow = new ARect();
		rootView.GetWindowVisibleDisplayFrame(visibleWindow);

		var editorBottom = location[1] + editorView.Height;
		var editorFocused = editorView.IsFocused;
		if (_editorBottom == editorBottom &&
			_visibleTop == visibleWindow.Top &&
			_visibleBottom == visibleWindow.Bottom &&
			_rootBottom == rootView.Height &&
			_editorWidth == editorView.Width &&
			_editorHeight == editorView.Height &&
			_editorFocused == editorFocused)
		{
			return;
		}

		_sequence++;
		_editorBottom = editorBottom;
		_visibleTop = visibleWindow.Top;
		_visibleBottom = visibleWindow.Bottom;
		_rootBottom = rootView.Height;
		_editorWidth = editorView.Width;
		_editorHeight = editorView.Height;
		_editorFocused = editorFocused;
		if (_editorFocused)
		{
			_lastFocusedSequence = _sequence;
			_lastFocusedEditorBottom = _editorBottom;
			_lastFocusedVisibleBottom = _visibleBottom;
		}

		UpdateMetricsLabel();
	}

	void UpdateMetricsLabel()
	{
		_metricsLabel.Text =
			$"seq={_sequence};bottom={_editorBottom};top={_visibleTop};visible={_visibleBottom};root={_rootBottom};focused={(_editorFocused ? 1 : 0)};width={_editorWidth};height={_editorHeight};snapshotSeq={_snapshotSequence};snapshotBottom={_snapshotEditorBottom};snapshotVisible={_snapshotVisibleBottom}";
	}

	protected override void OnDisappearing()
	{
		if (_isListening && _trackedEditor.Handler?.PlatformView is AView editorView)
		{
			var observer = editorView.ViewTreeObserver;
			if (observer is not null && observer.IsAlive)
				observer.RemoveOnGlobalLayoutListener(_globalLayoutListener);
		}

		_isListening = false;
		base.OnDisappearing();
	}

	sealed class GlobalLayoutListener : Java.Lang.Object, AViewTreeObserver.IOnGlobalLayoutListener
	{
		readonly Action _onGlobalLayout;

		public GlobalLayoutListener(Action onGlobalLayout)
		{
			_onGlobalLayout = onGlobalLayout;
		}

		public void OnGlobalLayout()
		{
			_onGlobalLayout();
		}
	}
#endif
}

