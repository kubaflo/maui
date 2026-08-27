namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding or showing elements in the ScrollView Scrolled event", PlatformAffected.Android)]
public partial class Issue27665 : ContentPage
{
#if ANDROID
	int _callbackCount = -1;
	int _goneCount;
	int _returnedVisibleCount;
	bool _pointerDown;
	bool _downObserved;
	bool _moveObserved;
	bool _upObserved;
	bool _positiveScrollObserved;
	bool _headersWereGone;
	bool _lastObservedVisible = true;
	bool _touchAttached;
#endif

	public Issue27665()
	{
		InitializeComponent();

#if ANDROID
		TestScrollView.HandlerChanged += (_, _) => AttachTouchObserver();
		EntryTest.HandlerChanged += (_, _) => UpdateTelemetry();
		ImageTest.HandlerChanged += (_, _) => UpdateTelemetry();
		ImageTest.Loaded += (_, _) => UpdateTelemetry();
		ImageTest.SizeChanged += (_, _) => UpdateTelemetry();
		ImageTest.PropertyChanged += (_, _) => UpdateTelemetry();
		Loaded += (_, _) =>
		{
			AttachTouchObserver();
			UpdateTelemetry();
		};
#endif
	}

	void ScrollView_OnScrolled(object sender, ScrolledEventArgs e)
	{
#if ANDROID
		_callbackCount = _callbackCount < 0 ? 1 : _callbackCount + 1;
		_positiveScrollObserved |= e.ScrollY > 0;
#endif

		bool showHeader = e.ScrollY <= 0;
		EntryTest.IsVisible = showHeader;
		ImageTest.IsVisible = showHeader;

#if ANDROID
		ObserveNativeHeaders();
		UpdateTelemetry();
#endif
	}

#if ANDROID
	void AttachTouchObserver()
	{
		if (_touchAttached ||
			TestScrollView.Handler?.PlatformView is not Microsoft.Maui.Platform.MauiScrollView nativeScrollView)
		{
			return;
		}

		_touchAttached = true;
		nativeScrollView.Touch += (_, args) =>
		{
			if (args.Event is null)
			{
				UpdateTelemetry();
				return;
			}

			switch (args.Event.ActionMasked)
			{
				case Android.Views.MotionEventActions.Down:
					_pointerDown = true;
					_downObserved = true;
					break;
				case Android.Views.MotionEventActions.Move:
					_moveObserved = true;
					break;
				case Android.Views.MotionEventActions.Up:
				case Android.Views.MotionEventActions.Cancel:
					_pointerDown = false;
					_upObserved = true;
					break;
			}

			ObserveNativeHeaders();
			UpdateTelemetry();
			args.Handled = false;
		};
	}

	void ObserveNativeHeaders()
	{
		if (EntryTest.Handler?.PlatformView is not Android.Views.View nativeEntry ||
			ImageTest.Handler?.PlatformView is not Android.Views.View nativeImage)
		{
			return;
		}

		bool headersVisible =
			nativeEntry.Visibility == Android.Views.ViewStates.Visible &&
			nativeImage.Visibility == Android.Views.ViewStates.Visible;
		bool headersGone =
			nativeEntry.Visibility == Android.Views.ViewStates.Gone &&
			nativeImage.Visibility == Android.Views.ViewStates.Gone;

		if (headersGone && !_headersWereGone)
		{
			_goneCount++;
			_headersWereGone = true;
		}

		if (headersVisible && !_lastObservedVisible && _pointerDown)
			_returnedVisibleCount++;

		if (headersVisible || headersGone)
			_lastObservedVisible = headersVisible;
	}

	void UpdateTelemetry()
	{
		if (TestScrollView.Handler?.PlatformView is not Microsoft.Maui.Platform.MauiScrollView nativeScrollView)
			return;

		bool ready =
			EntryTest.Handler?.PlatformView is Android.Views.View nativeEntry &&
			nativeEntry.Visibility == Android.Views.ViewStates.Visible &&
			ImageTest.Handler?.PlatformView is Android.Widget.ImageView nativeImage &&
			nativeImage.Visibility == Android.Views.ViewStates.Visible &&
			nativeImage.Drawable is Android.Graphics.Drawables.Drawable drawable &&
			drawable.IntrinsicWidth > 0 &&
			drawable.IntrinsicHeight > 0;

		nativeScrollView.ContentDescription =
			$"Ready={ready};CallbackObserved={_callbackCount >= 0};PositiveScroll={_positiveScrollObserved};" +
			$"GoneObserved={_goneCount > 0};ReturnedVisible={_returnedVisibleCount > 0};DownObserved={_downObserved};" +
			$"MoveObserved={_moveObserved};UpObserved={_upObserved}";
	}
#endif
}
