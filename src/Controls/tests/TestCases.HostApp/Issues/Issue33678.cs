#if ANDROID
using AView = Android.Views.View;
using AViewTreeObserver = Android.Views.ViewTreeObserver;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33678, "Edge to edge on Android doesn't work with Shell Navigation bar", PlatformAffected.Android)]
public class Issue33678 : ContentPage
{
	int _layoutSequence = -1;
	int _visibleBaselineSequence = -1;
	bool _hiddenLayoutRecorded;
	bool _navBarShown;
	bool _visibleLayoutRecorded;
	readonly Label _hiddenLayoutMarker;
	readonly Label _visibleLayoutMarker;
	AView _nativeContentView;
	NativeLayoutListener _nativeLayoutListener;
	ContentPage _shellPage;

	public Issue33678()
	{
		_hiddenLayoutMarker = new Label
		{
			AutomationId = "Issue33678HiddenLayout",
			Text = "HiddenLayout:-1",
			TextColor = Colors.White
		};

		_visibleLayoutMarker = new Label
		{
			AutomationId = "Issue33678VisibleLayout",
			Text = "VisibleLayout:-1",
			TextColor = Colors.White
		};

		var launchButton = new Button
		{
			AutomationId = "Issue33678Launch",
			Text = "Open Shell edge-to-edge scenario"
		};
		launchButton.Clicked += OnLaunchShellClicked;

		Content = new Grid
		{
			BackgroundColor = Color.FromArgb("#202030"),
			Children =
			{
				new VerticalStackLayout
				{
					Margin = new Thickness(24),
					Spacing = 18,
					VerticalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							FontSize = 24,
							HorizontalTextAlignment = TextAlignment.Center,
							Text = "Android Shell edge-to-edge",
							TextColor = Colors.White
						},
						launchButton
					}
				}
			}
		};
	}

	void OnLaunchShellClicked(object sender, EventArgs e)
	{
		var topBand = new Grid
		{
			AutomationId = "Issue33678TopBand",
			BackgroundColor = Color.FromArgb("#C43C5A"),
			HeightRequest = 170,
			VerticalOptions = LayoutOptions.Start,
			Children =
			{
				new Label
				{
					AutomationId = "Issue33678BandText",
					Margin = new Thickness(18, 12),
					Text = "EDGE-TO-EDGE CONTENT START",
					TextColor = Colors.White
				}
			}
		};

		var showNavBarButton = new Button
		{
			AutomationId = "Issue33678ShowNavBar",
			Text = "Show transparent Shell navigation bar"
		};
		showNavBarButton.Clicked += OnShowNavBarClicked;

		var controls = new VerticalStackLayout
		{
			Margin = new Thickness(24),
			Spacing = 14,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					FontSize = 22,
					HorizontalTextAlignment = TextAlignment.Center,
					Text = "Shell content should remain behind its transparent navigation bar",
					TextColor = Colors.White
				},
				showNavBarButton,
				_hiddenLayoutMarker,
				_visibleLayoutMarker
			}
		};

		var edgeToEdgeContent = new Grid
		{
			AutomationId = "Issue33678ContentRoot",
			BackgroundColor = Color.FromArgb("#303070"),
			Children =
			{
				topBand,
				controls
			}
		};
		edgeToEdgeContent.HandlerChanged += OnContentHandlerChanged;

		_shellPage = new ContentPage
		{
			Title = "Edge-to-edge",
			SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None,
			Content = edgeToEdgeContent
		};
		Shell.SetBackgroundColor(_shellPage, Colors.Transparent);
		Shell.SetNavBarHasShadow(_shellPage, false);
		Shell.SetNavBarIsVisible(_shellPage, false);

		var flyoutItem = new FlyoutItem { Title = "Edge-to-edge" };
		flyoutItem.Items.Add(new ShellContent
		{
			Title = "Edge-to-edge",
			Content = _shellPage
		});

		var shell = new Shell();
		shell.Items.Add(flyoutItem);
		Window.Page = shell;
	}

	void OnShowNavBarClicked(object sender, EventArgs e)
	{
		_visibleBaselineSequence = _layoutSequence;
		_navBarShown = true;
		Shell.SetNavBarIsVisible(_shellPage, true);
	}

	void OnContentHandlerChanged(object sender, EventArgs e)
	{
		if (sender is Grid grid && grid.Handler?.PlatformView is AView nativeView)
		{
			_nativeContentView = nativeView;
			_nativeLayoutListener = new NativeLayoutListener(this);
			nativeView.ViewTreeObserver.AddOnGlobalLayoutListener(_nativeLayoutListener);
		}
	}

	void OnNativeLayoutChanged()
	{
		_layoutSequence++;

		if (!_navBarShown && !_hiddenLayoutRecorded)
		{
			_hiddenLayoutRecorded = true;
			_hiddenLayoutMarker.Text = $"HiddenLayout:{_layoutSequence}";
		}
		else if (_navBarShown && !_visibleLayoutRecorded && _layoutSequence > _visibleBaselineSequence)
		{
			_visibleLayoutRecorded = true;
			_visibleLayoutMarker.Text = $"VisibleLayout:{_layoutSequence}";
			_nativeContentView.ViewTreeObserver.RemoveOnGlobalLayoutListener(_nativeLayoutListener);
		}
	}

	sealed class NativeLayoutListener : Java.Lang.Object, AViewTreeObserver.IOnGlobalLayoutListener
	{
		readonly Issue33678 _owner;

		public NativeLayoutListener(Issue33678 owner)
		{
			_owner = owner;
		}

		public void OnGlobalLayout()
		{
			_owner.OnNativeLayoutChanged();
		}
	}
}
#endif

