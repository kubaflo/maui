using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34538, "CollectionView items flicker when using async StreamImageSource with delayed stream", PlatformAffected.iOS)]
public class Issue34538 : ContentPage
{
	const string ImageData = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIW2P4v5ThPwAG7wKklwQ/bwAAAABJRU5ErkJggg==";
	const string PendingResult = "PENDING: Handler loading transitions not observed";
	const string PassResult = "PASS: Recycled delayed-stream images remained continuously visible";
	const string FailResult = "FAIL: A recycled delayed-stream image became blank and rendered again";

	readonly object _pendingLock = new();
	readonly List<PendingImage> _pendingImages = [];
	readonly Label _titleLabel;
	readonly Label _resultLabel;
	readonly CollectionView _collectionView;
	readonly NativeImageTracker _tracker;

	public Issue34538()
	{
		_titleLabel = new Label
		{
			AutomationId = "TitleLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "PENDING: Issue 34538 delayed stream images"
		};
		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = PendingResult
		};

		_tracker = new NativeImageTracker(CompleteObservation);

		var items = new ObservableCollection<ImageItem>(
			Enumerable.Range(0, 60).Select(index => new ImageItem(index, CreateSource(index))));

		_collectionView = new CollectionView
		{
			AutomationId = "ImageCollection",
			ItemsSource = items,
			ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical)
			{
				Span = 2
			},
			ItemTemplate = new DataTemplate(() =>
			{
				var image = new TrackedImage(_tracker);
				image.SetBinding(Image.SourceProperty, nameof(ImageItem.Source));

				return new Grid
				{
					HeightRequest = 180,
					Children = { image }
				};
			})
		};
		_collectionView.Scrolled += OnCollectionScrolled;

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Add(_titleLabel, 0, 0);
		grid.Add(_resultLabel, 0, 1);
		grid.Add(_collectionView, 0, 2);
		Content = grid;
	}

	StreamImageSource CreateSource(int index)
	{
		return new StreamImageSource
		{
			Stream = cancellationToken =>
			{
				var request = _tracker.RecordRequest(index);
				var completion = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
				lock (_pendingLock)
					_pendingImages.Add(new PendingImage(completion, cancellationToken, request));

				return completion.Task;
			}
		};
	}

	void OnCollectionScrolled(object sender, ItemsViewScrolledEventArgs e)
	{
		if (e.VerticalDelta < 0)
			_tracker.BeginReverseScroll();

		CompletePendingImages();

		if (e.VerticalDelta < 0 && e.FirstVisibleItemIndex == 0)
			_tracker.ObserveAfterReturningToTop();
	}

	void CompletePendingImages()
	{
		List<PendingImage> pending;
		lock (_pendingLock)
		{
			pending = [.. _pendingImages];
			_pendingImages.Clear();
		}

		foreach (var image in pending)
		{
			if (image.CancellationToken.IsCancellationRequested)
				image.Completion.TrySetCanceled(image.CancellationToken);
			else
				image.Completion.TrySetResult(new MemoryStream(Convert.FromBase64String(ImageData)));

			_tracker.RecordCompletion(image.Request);
		}
	}

	void CompleteObservation(bool initialRendered, bool reuseProven, bool sourceCorrelationProven, bool flickerObserved)
	{
		_titleLabel.Text = $"OBSERVED: initial={initialRendered}; reuse={reuseProven}; sourceCorrelated={sourceCorrelationProven}";
		_resultLabel.Text = flickerObserved ? FailResult : PassResult;
	}

	sealed class TrackedImage : Image
	{
		readonly NativeImageTracker _tracker;

		public TrackedImage(NativeImageTracker tracker)
		{
			_tracker = tracker;
			tracker.Register(this);
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			_tracker.Observe(this);
		}

		protected override void OnPropertyChanged(string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			if (propertyName == nameof(IsLoading))
				_tracker?.Observe(this);
		}
	}

	sealed class NativeImageTracker
	{
		readonly object _gate = new();
		readonly Action<bool, bool, bool, bool> _completed;
		readonly List<TrackedImage> _images = [];
		readonly List<NativeRequest> _requests = [];
		readonly Dictionary<TrackedImage, NativeRegistration> _registrations = [];
		readonly HashSet<int> _renderedItems = [];
		bool _reverseScroll;
		bool _returnedToTop;
		bool _initialRendered;
		bool _reuseProven;
		bool _sourceCorrelationProven;
		bool _flickerObserved;
		bool _observationCompleted;

		public NativeImageTracker(Action<bool, bool, bool, bool> completed)
		{
			_completed = completed;
		}

		public void Register(TrackedImage image)
		{
			lock (_gate)
				_images.Add(image);
		}

		public NativeRequest RecordRequest(int itemIndex)
		{
			lock (_gate)
			{
				var image = _images.LastOrDefault(candidate =>
					candidate.BindingContext is ImageItem item && item.Index == itemIndex);
				var platformView = image?.Handler?.PlatformView;
				var requestNumber = _requests.Count(request => request.ItemIndex == itemIndex) + 1;
				var request = new NativeRequest(itemIndex, requestNumber, image, platformView);
				_requests.Add(request);

				if (image is not null && requestNumber > 1 && image.IsLoading)
					RecordBlankTransition(image, request);

				return request;
			}
		}

		public void RecordCompletion(NativeRequest request)
		{
			lock (_gate)
			{
				request.Completed = true;
				if (request.Image is not null && request.PlatformView is not null)
					_sourceCorrelationProven = true;
				ObserveCore(request.Image);
				TryComplete();
			}
		}

		public void BeginReverseScroll()
		{
			lock (_gate)
				_reverseScroll = true;
		}

		public void ObserveAfterReturningToTop()
		{
			lock (_gate)
			{
				_returnedToTop = true;
				TryComplete();
			}
		}

		public void Observe(TrackedImage image)
		{
			lock (_gate)
			{
				ObserveCore(image);
				TryComplete();
			}
		}

		void ObserveCore(TrackedImage image)
		{
			var platformView = image.Handler?.PlatformView;
			if (image.BindingContext is not ImageItem item || platformView is null)
				return;

			if (!_registrations.TryGetValue(image, out var registration))
			{
				registration = new NativeRegistration();
				_registrations.Add(image, registration);
			}

			if (registration.ItemIndex >= 0 &&
				registration.ItemIndex != item.Index &&
				ReferenceEquals(registration.PlatformView, platformView))
				_reuseProven = true;

			registration.ItemIndex = item.Index;
			registration.PlatformView = platformView;

			var request = _requests.LastOrDefault(candidate =>
				candidate.ItemIndex == item.Index &&
				ReferenceEquals(candidate.Image, image) &&
				ReferenceEquals(candidate.PlatformView, platformView));

			if (!image.IsLoading && request?.Completed == true)
			{
				_initialRendered = true;
				_renderedItems.Add(item.Index);
				if (ReferenceEquals(registration.BlankRequest, request))
					_flickerObserved = true;
			}
			else if (_reverseScroll && item.Index % 2 != 0 && _renderedItems.Contains(item.Index) && request?.RequestNumber > 1)
			{
				RecordBlankTransition(image, request);
			}
		}

		void RecordBlankTransition(TrackedImage image, NativeRequest request)
		{
			if (!_registrations.TryGetValue(image, out var registration))
				return;

			registration.BlankRequest = request;
		}

		void TryComplete()
		{
			if (!_returnedToTop || _observationCompleted ||
				!_initialRendered || !_reuseProven || !_sourceCorrelationProven)
				return;

			if (!_flickerObserved &&
				(_requests.Any(request => !request.Completed) || _images.Any(image => image.IsLoading)))
				return;

			_observationCompleted = true;
			_completed(_initialRendered, _reuseProven, _sourceCorrelationProven, _flickerObserved);
		}
	}

	sealed class NativeRegistration
	{
		public int ItemIndex { get; set; } = -1;
		public object PlatformView { get; set; }
		public NativeRequest BlankRequest { get; set; }
	}

	sealed class NativeRequest
	{
		public NativeRequest(int itemIndex, int requestNumber, TrackedImage image, object platformView)
		{
			ItemIndex = itemIndex;
			RequestNumber = requestNumber;
			Image = image;
			PlatformView = platformView;
		}

		public int ItemIndex { get; }
		public int RequestNumber { get; }
		public TrackedImage Image { get; }
		public object PlatformView { get; }
		public bool Completed { get; set; }
	}

	public sealed class ImageItem
	{
		public ImageItem(int index, ImageSource source)
		{
			Index = index;
			Source = source;
		}

		public int Index { get; }
		public ImageSource Source { get; }
	}

	sealed class PendingImage
	{
		public PendingImage(TaskCompletionSource<Stream> completion, CancellationToken cancellationToken, NativeRequest request)
		{
			Completion = completion;
			CancellationToken = cancellationToken;
			Request = request;
		}

		public TaskCompletionSource<Stream> Completion { get; }
		public CancellationToken CancellationToken { get; }
		public NativeRequest Request { get; }
	}
}
