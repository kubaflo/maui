namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30834, "Shell TitleView children are cleared before the outgoing page unloads", PlatformAffected.Android)]
public class Issue30834 : Shell
{
	const string DetailRoute = "Issue30834Detail";
	readonly Issue30834State _state = new();

	public Issue30834()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		var landingPage = new Issue30834LandingPage(_state);

		Items.Add(new ShellContent
		{
			Title = "Issue 30834",
			Route = "Issue30834Home",
			Content = landingPage
		});

		Routing.RegisterRoute(DetailRoute, new Issue30834RouteFactory(_state));
		Navigated += OnNavigated;
	}

	void OnNavigated(object sender, ShellNavigatedEventArgs args)
	{
		if (!_state.DetailOpened ||
			(args.Source != ShellNavigationSource.Pop && args.Source != ShellNavigationSource.PopToRoot))
			return;

		_state.NavigationCompleted = true;
		Dispatcher.Dispatch(_state.TryPublishObservation);
	}

	sealed class Issue30834RouteFactory : RouteFactory
	{
		readonly Issue30834State _state;

		public Issue30834RouteFactory(Issue30834State state)
		{
			_state = state;
		}

		public override Element GetOrCreate() => CreatePage();

		public override Element GetOrCreate(IServiceProvider services) => CreatePage();

		Element CreatePage()
		{
			int pageId = ++_state.LastPageId;
			_state.ExpectedPageId = pageId;
			return new Issue30834DetailPage(_state, pageId);
		}
	}

	sealed class Issue30834LandingPage : ContentPage
	{
		readonly Issue30834State _state;
		readonly Label _observationReady;
		readonly Label _unloadObservation;
		readonly Label _postPopCallback;
		readonly Label _expectedPageId;
		readonly Label _observedPageId;
		readonly Label _titleLabelAttached;
		readonly Label _titleButtonAttached;
		readonly Label _pageLoaded;
		readonly Label _contentAttached;
		readonly Label _earlyDetachCount;

		public Issue30834LandingPage(Issue30834State state)
		{
			_state = state;
			Title = "Issue 30834";
			_state.PublishObservation = PublishObservation;

			var openDetail = new Button
			{
				Text = "Open detail page",
				AutomationId = "OpenDetail"
			};
			openDetail.Clicked += async (_, _) =>
			{
				_state.ResetObservation();
				_state.DetailOpened = true;
				await Shell.Current.GoToAsync(DetailRoute);
			};

			_observationReady = CreateStatusLabel("PostPopObservationReady", "-1");
			_observationReady.IsVisible = false;
			_unloadObservation = CreateStatusLabel("UnloadObservation", "-1");
			_postPopCallback = CreateStatusLabel("PostPopCallback", "-1");
			_expectedPageId = CreateStatusLabel("ExpectedPageId", "-1");
			_observedPageId = CreateStatusLabel("ObservedPageId", "-1");
			_titleLabelAttached = CreateStatusLabel("ObservedTitleLabelAttached", "-1");
			_titleButtonAttached = CreateStatusLabel("ObservedTitleButtonAttached", "-1");
			_pageLoaded = CreateStatusLabel("ObservedPageLoaded", "-1");
			_contentAttached = CreateStatusLabel("ObservedContentAttached", "-1");
			_earlyDetachCount = CreateStatusLabel("EarlyDetachCount", "-1");

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					openDetail,
					_observationReady,
					_unloadObservation,
					_postPopCallback,
					_expectedPageId,
					_observedPageId,
					_titleLabelAttached,
					_titleButtonAttached,
					_pageLoaded,
					_contentAttached,
					_earlyDetachCount
				}
			};
		}

		public void PublishObservation()
		{
			_unloadObservation.Text = _state.UnloadObservation.ToString();
			_postPopCallback.Text = _state.PostPopCallback.ToString();
			_expectedPageId.Text = _state.ExpectedPageId.ToString();
			_observedPageId.Text = _state.ObservedPageId.ToString();
			_titleLabelAttached.Text = _state.TitleLabelAttached.ToString();
			_titleButtonAttached.Text = _state.TitleButtonAttached.ToString();
			_pageLoaded.Text = _state.PageLoaded.ToString();
			_contentAttached.Text = _state.ContentAttached.ToString();
			_earlyDetachCount.Text = _state.EarlyDetachCount.ToString();
			_observationReady.Text = "Ready";
			_observationReady.IsVisible = true;
		}

		static Label CreateStatusLabel(string automationId, string text) =>
			new()
			{
				AutomationId = automationId,
				Text = text
			};
	}

	sealed class Issue30834DetailPage : ContentPage
	{
		readonly Issue30834State _state;
		readonly int _pageId;
		readonly Grid _titleGrid;
		readonly Label _titleLabel;
		readonly Button _titleButton;
		readonly Label _contentProbe;
		readonly Label _initialProbeReady;
		readonly Label _initialLabelAttached;
		readonly Label _initialButtonAttached;
		readonly Label _initialContentAttached;
		readonly Label _interactionStatus;

		public Issue30834DetailPage(Issue30834State state, int pageId)
		{
			_state = state;
			_pageId = pageId;
			Title = "Detail page";

			_titleLabel = new Label
			{
				Text = "New Page",
				AutomationId = "TitleLabel"
			};
			_titleButton = new Button
			{
				Text = "Demo button",
				AutomationId = "TitleButton"
			};
			_titleGrid = new Grid
			{
				AutomationId = "TitleGrid",
				ColumnDefinitions =
				[
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				]
			};
			_titleGrid.Add(_titleLabel);
			_titleGrid.Add(_titleButton, 1);
			Shell.SetTitleView(this, _titleGrid);

			_contentProbe = new Label
			{
				Text = "Detail content",
				AutomationId = "ContentProbe"
			};
			_initialProbeReady = new Label
			{
				Text = "-1",
				AutomationId = "InitialProbeReady",
				IsVisible = false
			};
			_initialLabelAttached = CreateStatusLabel("InitialTitleLabelAttached");
			_initialButtonAttached = CreateStatusLabel("InitialTitleButtonAttached");
			_initialContentAttached = CreateStatusLabel("InitialContentAttached");
			_interactionStatus = CreateStatusLabel("TitleInteractionStatus");

			_titleButton.Clicked += (_, _) =>
			{
				_state.ObservationArmed = true;
				_interactionStatus.Text = "1";
			};
			_titleGrid.Unloaded += OnTitleGridUnloaded;
			_titleLabel.Loaded += (_, _) => Dispatcher.Dispatch(PublishInitialAttachment);
			_titleButton.Loaded += (_, _) => Dispatcher.Dispatch(PublishInitialAttachment);
			_contentProbe.Loaded += (_, _) => Dispatcher.Dispatch(PublishInitialAttachment);

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					_contentProbe,
					_initialProbeReady,
					_initialLabelAttached,
					_initialButtonAttached,
					_initialContentAttached,
					_interactionStatus
				}
			};
		}

		void PublishInitialAttachment()
		{
			bool labelAttached = IsNativeAttached(_titleLabel);
			bool buttonAttached = IsNativeAttached(_titleButton);
			bool contentAttached = IsNativeAttached(_contentProbe);

			_initialLabelAttached.Text = labelAttached.ToString();
			_initialButtonAttached.Text = buttonAttached.ToString();
			_initialContentAttached.Text = contentAttached.ToString();

			if (labelAttached && buttonAttached && contentAttached)
			{
				_initialProbeReady.Text = "Ready";
				_initialProbeReady.IsVisible = true;
			}
		}

		void OnTitleGridUnloaded(object sender, EventArgs args)
		{
			if (_state.ObservationArmed && _state.UnloadObservation < 0)
			{
				_state.UnloadObservation = 0;
				_state.ObservedPageId = _pageId;
				_state.TitleLabelAttached = IsNativeAttached(_titleLabel);
				_state.TitleButtonAttached = IsNativeAttached(_titleButton);
				_state.PageLoaded = IsLoaded;
				_state.ContentAttached = IsNativeAttached(_contentProbe);
				_state.EarlyDetachCount = _state.PageLoaded
					? (_state.TitleLabelAttached ? 0 : 1) + (_state.TitleButtonAttached ? 0 : 1)
					: 0;
			}

			Dispatcher.Dispatch(_state.TryPublishObservation);
		}

		static Label CreateStatusLabel(string automationId) =>
			new()
			{
				AutomationId = automationId,
				Text = "-1"
			};

		static bool IsNativeAttached(VisualElement element)
		{
#if ANDROID
			if (element.Handler is null ||
				element.Handler.PlatformView is not Android.Views.View platformView)
			{
				return false;
			}

			return platformView.IsAttachedToWindow;
#else
			return element.IsLoaded;
#endif
		}
	}

	sealed class Issue30834State
	{
		public bool DetailOpened { get; set; }
		public bool ObservationArmed { get; set; }
		public bool NavigationCompleted { get; set; }
		public bool ObservationPublished { get; set; }
		public int LastPageId { get; set; }
		public int ExpectedPageId { get; set; }
		public int UnloadObservation { get; set; }
		public int PostPopCallback { get; set; }
		public int ObservedPageId { get; set; }
		public bool TitleLabelAttached { get; set; }
		public bool TitleButtonAttached { get; set; }
		public bool PageLoaded { get; set; }
		public bool ContentAttached { get; set; }
		public int EarlyDetachCount { get; set; }
		public Action PublishObservation { get; set; } = () => { };

		public void TryPublishObservation()
		{
			if (!NavigationCompleted || UnloadObservation < 0 || ObservationPublished)
				return;

			ObservationPublished = true;
			PostPopCallback++;
			PublishObservation();
		}

		public void ResetObservation()
		{
			NavigationCompleted = false;
			ObservationArmed = false;
			ObservationPublished = false;
			ExpectedPageId = -1;
			UnloadObservation = -1;
			PostPopCallback = -1;
			ObservedPageId = -1;
			TitleLabelAttached = false;
			TitleButtonAttached = false;
			PageLoaded = false;
			ContentAttached = false;
			EarlyDetachCount = -1;
		}
	}
}

