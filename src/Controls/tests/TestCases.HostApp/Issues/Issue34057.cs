#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "Animation accesses disposed services after closing a child window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	const string InitialTelemetry = "Loaded=-1;SceneVerified=-1;Disappearing=-1;CloseReturned=-1;AnimationReturned=-1;ExceptionType=None;ObjectName=None;Completed=-1";

	readonly Label _telemetryLabel;
	bool _triggered;
	int _loaded = -1;
	int _sceneVerified = -1;
	int _disappearing = -1;
	int _closeReturned = -1;
	int _animationReturned = -1;
	int _completed = -1;
	string _exceptionType = "None";
	string _objectName = "None";

	public Issue34057()
	{
		var openChildWindowButton = new Button
		{
			AutomationId = "Issue34057OpenChildWindowButton",
			Text = "Open and close image editor window"
		};
		openChildWindowButton.Clicked += OnOpenChildWindowClicked;

		_telemetryLabel = new Label
		{
			AutomationId = "Issue34057Telemetry",
			Text = InitialTelemetry
		};

		Content = new Grid
		{
			Padding = 32,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				openChildWindowButton,
				_telemetryLabel
			}
		};

		Grid.SetRow(_telemetryLabel, 1);
	}

	void OnOpenChildWindowClicked(object sender, EventArgs e)
	{
		if (_triggered)
			return;

		_triggered = true;

		var popupContent = new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					Text = "Save image"
				},
				new Label { Text = "Save popup is visible" }
			}
		};
		var savePopup = new Border
		{
			BackgroundColor = Colors.White,
			Padding = 20,
			Content = popupContent
		};
		var editorSurface = new Border
		{
			BackgroundColor = Colors.Black,
			Content = new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				Text = "Image editor surface",
				TextColor = Colors.White,
				VerticalOptions = LayoutOptions.Center
			}
		};
		var childGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				editorSurface,
				savePopup
			}
		};
		Grid.SetRow(savePopup, 1);

		var childPage = new ContentPage
		{
			Title = "Image editor",
			Content = childGrid
		};
		var childNavigation = new NavigationPage(childPage);
		var childWindow = new Window(childNavigation)
		{
			Title = "Image editor"
		};
		var application = Application.Current;
		if (application is null)
			throw new InvalidOperationException("Application.Current must be available to open the child window.");

		childPage.Disappearing += (_, _) =>
		{
			_disappearing = 1;
			UpdateTelemetry();
			childPage.Dispatcher.Dispatch(() => StartPendingPopupAnimation(savePopup));
		};

		savePopup.Loaded += OnSavePopupLoaded;

		application.OpenWindow(childWindow);

		void OnSavePopupLoaded(object loadedSender, EventArgs loadedArgs)
		{
			savePopup.Loaded -= OnSavePopupLoaded;
			childPage.Dispatcher.Dispatch(() =>
			{
				_loaded = 1;
				_sceneVerified =
					childPage.IsLoaded &&
					childNavigation.IsLoaded &&
					editorSurface.IsLoaded &&
					savePopup.IsLoaded &&
					ReferenceEquals(savePopup.Parent, childGrid) &&
					savePopup.BackgroundColor == Colors.White &&
					savePopup.Padding.Left == 20 &&
					savePopup.Padding.Top == 20 &&
					savePopup.Padding.Right == 20 &&
					savePopup.Padding.Bottom == 20 &&
					popupContent.Children.Count == 2
						? 1
						: 0;
				UpdateTelemetry();

				application.CloseWindow(childWindow);
				_closeReturned = 1;
				UpdateTelemetry();
			});
		}
	}

	void StartPendingPopupAnimation(Border savePopup)
	{
		try
		{
			IAnimatable popupAnimationTarget = savePopup;
			AnimationExtensions.Animate<double>(
				popupAnimationTarget,
				"HideSavePopup",
				value => 1 - value,
				opacity => savePopup.Opacity = opacity,
				length: 250);
			_animationReturned = 1;
		}
		catch (ObjectDisposedException ex) when (ex.ObjectName == "IServiceProvider")
		{
			_animationReturned = 0;
			_exceptionType = nameof(ObjectDisposedException);
			_objectName = "IServiceProvider";
		}

		_completed = 1;
		UpdateTelemetry();
	}

	void UpdateTelemetry()
	{
		_telemetryLabel.Text =
			$"Loaded={_loaded};SceneVerified={_sceneVerified};Disappearing={_disappearing};CloseReturned={_closeReturned};" +
			$"AnimationReturned={_animationReturned};ExceptionType={_exceptionType};ObjectName={_objectName};Completed={_completed}";
	}
}
#endif

