using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AViewTreeObserver = Android.Views.ViewTreeObserver;

namespace Maui.Controls.Sample.Issues;

public partial class Issue27665
{
	VisibilityObserver _visibilityObserver = null!;
	EventHandler _handlerChanged = null!;

	partial void StartNativeObservation()
	{
		_handlerChanged = (_, _) => TryStartNativeObservation();
		EntryTest.HandlerChanged += _handlerChanged;
		ImageTest.HandlerChanged += _handlerChanged;
		ScrollHost.HandlerChanged += _handlerChanged;
	}

	void TryStartNativeObservation()
	{
		if (_visibilityObserver is not null ||
			EntryTest.Handler?.PlatformView is not AView nativeEntry ||
			ImageTest.Handler?.PlatformView is not ImageView nativeImage ||
			ScrollHost.Handler?.PlatformView is not MauiScrollView nativeScroll)
		{
			return;
		}

		_visibilityObserver = new VisibilityObserver(nativeEntry, nativeImage, nativeScroll, NativeState);
		nativeScroll.ViewTreeObserver.AddOnPreDrawListener(_visibilityObserver);
	}

	partial void RecordNativeScroll()
	{
		if (_visibilityObserver is not null)
			_visibilityObserver.RecordScroll();
	}

	partial void StopNativeObservation()
	{
		EntryTest.HandlerChanged -= _handlerChanged;
		ImageTest.HandlerChanged -= _handlerChanged;
		ScrollHost.HandlerChanged -= _handlerChanged;

		if (_visibilityObserver is null)
			return;

		if (_visibilityObserver.ScrollView.ViewTreeObserver.IsAlive)
			_visibilityObserver.ScrollView.ViewTreeObserver.RemoveOnPreDrawListener(_visibilityObserver);

		_visibilityObserver.Dispose();
		_visibilityObserver = null!;
	}

	sealed class VisibilityObserver : Java.Lang.Object, AViewTreeObserver.IOnPreDrawListener
	{
		readonly AView _entry;
		readonly ImageView _image;
		readonly Label _state;
		ViewStates _entryState;
		ViewStates _imageState;
		int _preDrawCount;
		int _entryTransitions = -1;
		int _imageTransitions = -1;
		int _scrollCallbacks = -1;
		int _maximumOffset = -1;
		bool _armed;
		bool _postTriggerDraw;

		public VisibilityObserver(AView entry, ImageView image, MauiScrollView scrollView, Label state)
		{
			_entry = entry;
			_image = image;
			ScrollView = scrollView;
			_state = state;
		}

		public MauiScrollView ScrollView { get; }

		public bool OnPreDraw()
		{
			_preDrawCount++;

			if (!_armed)
			{
				var content = ScrollView.GetChildAt(0);
				if (_preDrawCount < 3 || _image.Drawable is null || content is null || content.Height <= ScrollView.Height)
					return true;

				_entryState = _entry.Visibility;
				_imageState = _image.Visibility;
				_entryTransitions = 0;
				_imageTransitions = 0;
				_scrollCallbacks = 0;
				_maximumOffset = 0;
				_armed = true;
				PublishState();
				return true;
			}

			bool stateChanged = false;
			if (_entry.Visibility != _entryState)
			{
				_entryState = _entry.Visibility;
				_entryTransitions++;
				stateChanged = true;
			}

			if (_image.Visibility != _imageState)
			{
				_imageState = _image.Visibility;
				_imageTransitions++;
				stateChanged = true;
			}

			if (_scrollCallbacks > 0 && !_postTriggerDraw)
			{
				_postTriggerDraw = true;
				stateChanged = true;
			}

			if (stateChanged)
				PublishState();

			return true;
		}

		public void RecordScroll()
		{
			if (!_armed)
				return;

			_scrollCallbacks++;
			_maximumOffset = Math.Max(_maximumOffset, ScrollView.ScrollY);
		}

		void PublishState()
		{
			var content = ScrollView.GetChildAt(0);
			int contentHeight = content?.Height ?? 0;
			_state.Text = $"Ready=1;EntryState={_entryState};EntryTransitions={_entryTransitions};ImageState={_imageState};ImageTransitions={_imageTransitions};ScrollCallbacks={_scrollCallbacks};MaxOffset={_maximumOffset};PostTriggerDraw={(_postTriggerDraw ? 1 : 0)};ContentHeight={contentHeight};ViewportHeight={ScrollView.Height};ImageLoaded={(_image.Drawable is not null ? 1 : 0)}";
		}
	}
}
