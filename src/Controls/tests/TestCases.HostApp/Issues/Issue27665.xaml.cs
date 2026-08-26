#if ANDROID
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding or showing elements in the ScrollView Scrolled event on Android", PlatformAffected.Android)]
public partial class Issue27665 : ContentPage
{
#if ANDROID
	int _callbackCount = -1;
	int _entryTransitionCount;
	int _imageTransitionCount;
	int _lastEntryVisibility;
	int _lastImageVisibility;
	int _maximumNativeOffset;
	bool _nativeStateReady;
	string _initialState = string.Empty;
#endif

	public Issue27665()
	{
		InitializeComponent();
		Loaded += OnLoaded;
#if ANDROID
		imageTest.PropertyChanged += OnImagePropertyChanged;
#endif
	}

	void OnLoaded(object sender, EventArgs e)
	{
#if ANDROID
		InitializeNativeState();
#endif
	}

#if ANDROID
	void OnImagePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Image.IsLoading) && !imageTest.IsLoading)
			InitializeNativeState();
	}

	void InitializeNativeState()
	{
		if (_nativeStateReady)
			return;

		if (!TryReadNativeState(out var entryView, out var imageView, out var nativeScrollView))
		{
			PublishDiagnosticState("setup=Native handlers were not attached");
			return;
		}

		var nativeImageHasContent = imageView is Android.Widget.ImageView imageViewControl && imageViewControl.Drawable is not null;
		if (imageTest.IsLoading || !nativeImageHasContent)
		{
			PublishDiagnosticState("setup=Waiting for native image content");
			return;
		}

		_lastEntryVisibility = (int)entryView.Visibility;
		_lastImageVisibility = (int)imageView.Visibility;
		_nativeStateReady = true;

		_initialState =
			$"initialCallback=-1|initialEntryTransitions=-1|initialImageTransitions=-1" +
			$"|initialEntryVisibility={entryView.Visibility}|initialImageVisibility={imageView.Visibility}" +
			$"|initialOffset={nativeScrollView.ScrollY}|initialEntryId={RuntimeHelpers.GetHashCode(entryView)}" +
			$"|initialImageId={RuntimeHelpers.GetHashCode(imageView)}|imageHasContent={nativeImageHasContent}" +
			$"|item20Text={item20.Text}|item20BelowHeading={item20.Y > heading.Y}";
		PublishDiagnosticState(
			$"{_initialState}" +
			$"|currentCallback=-1|entryTransitions=0|imageTransitions=0|currentOffset={nativeScrollView.ScrollY}" +
			$"|maximumOffset={nativeScrollView.ScrollY}|currentEntryVisibility={entryView.Visibility}" +
			$"|currentImageVisibility={imageView.Visibility}|currentEntryId={RuntimeHelpers.GetHashCode(entryView)}" +
			$"|currentImageId={RuntimeHelpers.GetHashCode(imageView)}");
	}
#endif

	void ScrollView_OnScrolled(object sender, ScrolledEventArgs e)
	{
#if ANDROID
		Android.Views.View entryView = null!;
		Android.Views.View imageView = null!;
		Microsoft.Maui.Platform.MauiScrollView nativeScrollView = null!;
		var hasNativeState = _nativeStateReady && TryReadNativeState(out entryView, out imageView, out nativeScrollView);
		var nativeOffsetBeforeUpdate = hasNativeState ? nativeScrollView.ScrollY : 0;
#endif

		var shouldBeVisible = e.ScrollY <= 0;
		entryTest.IsVisible = shouldBeVisible;
		imageTest.IsVisible = shouldBeVisible;

#if ANDROID
		if (!hasNativeState)
			return;

		_callbackCount++;
		_entryTransitionCount += RecordTransition(ref _lastEntryVisibility, entryView.Visibility);
		_imageTransitionCount += RecordTransition(ref _lastImageVisibility, imageView.Visibility);
		_maximumNativeOffset = Math.Max(_maximumNativeOffset, Math.Max(nativeOffsetBeforeUpdate, nativeScrollView.ScrollY));

		PublishDiagnosticState(
			$"{_initialState}|currentCallback={_callbackCount}|entryTransitions={_entryTransitionCount}" +
			$"|imageTransitions={_imageTransitionCount}|currentOffset={nativeScrollView.ScrollY}" +
			$"|maximumOffset={_maximumNativeOffset}|currentEntryVisibility={entryView.Visibility}" +
			$"|currentImageVisibility={imageView.Visibility}|currentEntryId={RuntimeHelpers.GetHashCode(entryView)}" +
			$"|currentImageId={RuntimeHelpers.GetHashCode(imageView)}");
#endif
	}

#if ANDROID
	void PublishDiagnosticState(string state) =>
		SemanticProperties.SetDescription(scrollView, state);

	static int RecordTransition(ref int previousVisibility, Android.Views.ViewStates currentVisibility)
	{
		var currentValue = (int)currentVisibility;
		if (currentValue == previousVisibility)
			return 0;

		previousVisibility = currentValue;
		return 1;
	}

	bool TryReadNativeState(
		out Android.Views.View entryView,
		out Android.Views.View imageView,
		out Microsoft.Maui.Platform.MauiScrollView nativeScrollView)
	{
		if (entryTest.Handler?.PlatformView is Android.Views.View resolvedEntryView &&
			imageTest.Handler?.PlatformView is Android.Views.View resolvedImageView &&
			scrollView.Handler?.PlatformView is Microsoft.Maui.Platform.MauiScrollView resolvedScrollView)
		{
			entryView = resolvedEntryView;
			imageView = resolvedImageView;
			nativeScrollView = resolvedScrollView;
			return true;
		}

		entryView = null!;
		imageView = null!;
		nativeScrollView = null!;
		return false;
	}
#endif
}
