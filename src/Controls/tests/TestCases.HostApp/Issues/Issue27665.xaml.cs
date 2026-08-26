namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding and showing elements from ScrollView.Scrolled on Android", PlatformAffected.Android)]
public partial class Issue27665 : ContentPage
{
#if ANDROID
	VisibilityObserver _visibilityObserver = null!;
#endif
	int _scrollCallbacks;
	int _lastCallbackScrollY = -1;

	public Issue27665()
	{
		InitializeComponent();
#if ANDROID
		Loaded += OnLoaded;
#endif
	}

	void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
	{
		_scrollCallbacks++;
		_lastCallbackScrollY = (int)Math.Round(e.ScrollY);

		bool showHeader = e.ScrollY <= 0;
		HeaderEntry.IsVisible = showHeader;
		HeaderImage.IsVisible = showHeader;

#if ANDROID
		_visibilityObserver?.PublishTelemetry();
#endif
	}

#if ANDROID
	void OnLoaded(object sender, EventArgs e)
	{
		if (_visibilityObserver is not null)
			return;

		if (HeaderEntry.Handler?.PlatformView is not Android.Views.View entryNativeView ||
			HeaderImage.Handler?.PlatformView is not Android.Widget.ImageView imageNativeView ||
			ScrollArea.Handler?.PlatformView is not Android.Views.View scrollNativeView)
		{
			throw new InvalidOperationException("The Android handlers were not attached when the page loaded.");
		}

		_visibilityObserver = new VisibilityObserver(
			this,
			entryNativeView,
			imageNativeView,
			scrollNativeView);
		scrollNativeView.ViewTreeObserver.AddOnPreDrawListener(_visibilityObserver);
		_visibilityObserver.PublishTelemetry();
	}

	sealed class VisibilityObserver : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnPreDrawListener
	{
		readonly Issue27665 _page;
		readonly Android.Views.View _entryNativeView;
		readonly Android.Widget.ImageView _imageNativeView;
		readonly Android.Views.View _scrollNativeView;
		int _frames;
		int _postTriggerFrames;
		int _entryHiddenFrames;
		int _imageHiddenFrames;
		int _entryReappearances;
		int _imageReappearances;
		bool _previousEntryVisible = true;
		bool _previousImageVisible = true;

		public VisibilityObserver(
			Issue27665 page,
			Android.Views.View entryNativeView,
			Android.Widget.ImageView imageNativeView,
			Android.Views.View scrollNativeView)
		{
			_page = page;
			_entryNativeView = entryNativeView;
			_imageNativeView = imageNativeView;
			_scrollNativeView = scrollNativeView;
		}

		public bool OnPreDraw()
		{
			_frames++;
			if (_page._scrollCallbacks > 0)
				_postTriggerFrames++;

			bool entryVisible = _entryNativeView.Visibility == Android.Views.ViewStates.Visible;
			bool imageVisible = _imageNativeView.Visibility == Android.Views.ViewStates.Visible;

			if (!entryVisible)
				_entryHiddenFrames = 1;
			else if (!_previousEntryVisible)
				_entryReappearances++;

			if (!imageVisible)
				_imageHiddenFrames = 1;
			else if (!_previousImageVisible)
				_imageReappearances++;

			_previousEntryVisible = entryVisible;
			_previousImageVisible = imageVisible;
			PublishTelemetry();
			if (_frames < 3 || (_page._scrollCallbacks > 0 && _postTriggerFrames < 5))
				_scrollNativeView.PostInvalidateOnAnimation();
			return true;
		}

		public void PublishTelemetry()
		{
			int reportedFrames = Math.Min(_frames, 3);
			int handlersAttached =
				_entryNativeView.IsAttachedToWindow &&
				_imageNativeView.IsAttachedToWindow &&
				_scrollNativeView.IsAttachedToWindow ? 1 : 0;
			int imageLoaded = _imageNativeView.Drawable is null ? 0 : 1;
			int currentScrollY = (int)Math.Round(_page.ScrollArea.ScrollY);
			int touchSlop = Android.Views.ViewConfiguration.Get(_scrollNativeView.Context).ScaledTouchSlop;
			string telemetry =
				$"attached={handlersAttached};asset={imageLoaded};touchSlop={touchSlop};frames={reportedFrames};postFrames={Math.Min(_postTriggerFrames, 5)};callbacks={_page._scrollCallbacks};y={currentScrollY};callbackY={_page._lastCallbackScrollY};" +
				$"entryHidden={_entryHiddenFrames};imageHidden={_imageHiddenFrames};entryReappear={_entryReappearances};imageReappear={_imageReappearances}";

			if (SemanticProperties.GetDescription(_page.ScrollArea) != telemetry)
				SemanticProperties.SetDescription(_page.ScrollArea, telemetry);
		}
	}
#endif
}
