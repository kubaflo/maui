#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	Window _childWindow = null!;
	Border _savePopup = null!;
	readonly Button _closeChildButton;
	readonly Label _childLoadedStatus;
	readonly Label _popupIdentityStatus;
	readonly Label _destructionStatus;
	readonly Label _reactivationStatus;
	readonly Label _animationState;
	readonly Label _animationFinishedStatus;
	bool _childDestroyed;
	bool _rootReactivated;
	bool _animationStarted;

	public Issue34057()
	{
		var openChildButton = new Button
		{
			Text = "Open child window",
			AutomationId = "OpenChildButton"
		};
		openChildButton.Clicked += OnOpenChildClicked;

		_closeChildButton = new Button
		{
			Text = "Close child window",
			AutomationId = "CloseChildButton",
			IsEnabled = false
		};
		_closeChildButton.Clicked += OnCloseChildClicked;

		_childLoadedStatus = CreateStatusLabel("ChildLoadedStatus", "Child not loaded");
		_popupIdentityStatus = CreateStatusLabel("PopupIdentityStatus", "Popup not created");
		_destructionStatus = CreateStatusLabel("ChildDestructionStatus", "Not destroyed");
		_reactivationStatus = CreateStatusLabel("RootReactivationStatus", "Not reactivated");
		_animationState = CreateStatusLabel("AnimationState", "Not started");
		_animationFinishedStatus = CreateStatusLabel("AnimationFinishedStatus", "Finished");
		_animationFinishedStatus.IsVisible = false;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Child-window animation after close",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				},
				new Label
				{
					Text = "Open the side-by-side child popup, then close its window from these controls.",
					FontSize = 16
				},
				openChildButton,
				_closeChildButton,
				_childLoadedStatus,
				_popupIdentityStatus,
				_destructionStatus,
				_reactivationStatus,
				_animationState,
				_animationFinishedStatus
			}
		};
	}

	static Label CreateStatusLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text,
			FontSize = 16
		};

	void OnOpenChildClicked(object sender, EventArgs e)
	{
		_savePopup = new Border
		{
			AutomationId = "SavePopup",
			BackgroundColor = Colors.White,
			Padding = 20,
			Content = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "Save image?",
						FontSize = 22,
						FontAttributes = FontAttributes.Bold,
						TextColor = Colors.Black
					},
					new Label
					{
						Text = "This popup will animate when its child window closes.",
						TextColor = Colors.Black
					}
				}
			}
		};

		var editorSurface = new Border
		{
			AutomationId = "ImageEditorSurface",
			BackgroundColor = Colors.Black,
			Margin = new Thickness(0, 16),
			Content = _savePopup
		};
		Grid.SetRow(editorSurface, 1);

		var childGrid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				new Label
				{
					Text = "Child window ready",
					AutomationId = "ChildReadyStatus",
					TextColor = Colors.White,
					FontSize = 20
				},
				editorSurface
			}
		};

		var childPage = new ContentPage
		{
			Title = "Image viewer",
			BackgroundColor = Colors.DarkSlateGray,
			Content = childGrid
		};
		childPage.Loaded += (_, _) =>
		{
			_childLoadedStatus.Text = "Child page loaded";
			_popupIdentityStatus.Text =
				_savePopup.IsLoaded && ReferenceEquals(editorSurface.Content, _savePopup)
					? "SavePopup loaded"
					: "Popup not loaded";
		};

		_childWindow = new Window(new NavigationPage(childPage))
		{
			Title = "Image viewer child window",
			X = 650,
			Y = 100,
			Width = 500,
			Height = 500
		};
		_childWindow.Destroying += OnChildWindowDestroying;

		var rootWindow = Window;
		if (rootWindow is null)
		{
			_animationState.Text = "Root window unavailable";
			_animationFinishedStatus.IsVisible = true;
			return;
		}

		rootWindow.X = 50;
		rootWindow.Y = 100;
		rootWindow.Width = 500;
		rootWindow.Height = 500;

		var application = Application.Current;
		if (application is null)
		{
			_animationState.Text = "Application unavailable";
			_animationFinishedStatus.IsVisible = true;
			return;
		}

		application.OpenWindow(_childWindow);
		application.ActivateWindow(rootWindow);
		_closeChildButton.IsEnabled = true;
	}

	void OnCloseChildClicked(object sender, EventArgs e)
	{
		var rootWindow = Window;
		var application = Application.Current;
		if (rootWindow is null || application is null)
		{
			_animationState.Text = "Close prerequisites unavailable";
			_animationFinishedStatus.IsVisible = true;
			return;
		}

		rootWindow.Activated += OnRootActivatedAfterChildClose;
		application.ActivateWindow(_childWindow);
		application.CloseWindow(_childWindow);
		_closeChildButton.IsEnabled = false;
	}

	void OnChildWindowDestroying(object sender, EventArgs e)
	{
		_childDestroyed = true;
		_destructionStatus.Text = "Destroyed";
	}

	void OnRootActivatedAfterChildClose(object sender, EventArgs e)
	{
		if (sender is Window rootWindow)
			rootWindow.Activated -= OnRootActivatedAfterChildClose;

		_rootReactivated = true;
		_reactivationStatus.Text = "Reactivated";
		StartPopupAnimationAfterDestruction();
	}

	void StartPopupAnimationAfterDestruction()
	{
		if (!_childDestroyed)
		{
			_animationState.Text = "Reactivated before destruction";
			_animationFinishedStatus.IsVisible = true;
			return;
		}

		if (!_rootReactivated || _animationStarted)
			return;

		_animationStarted = true;
		var savePopup = _savePopup;
		IAnimatable animatable = savePopup;

		try
		{
			AnimationExtensions.Animate<double>(
				animatable,
				"HidePopup",
				progress => progress,
				progress => savePopup.Opacity = 1 - progress,
				rate: 16,
				length: 250,
				finished: (_, canceled) =>
				{
					_animationState.Text = canceled ? "Canceled" : "Completed";
					_animationFinishedStatus.IsVisible = true;
				});
		}
		catch (ObjectDisposedException)
		{
			_animationState.Text = "ObjectDisposedException";
			_animationFinishedStatus.IsVisible = true;
		}
	}
}
#endif

