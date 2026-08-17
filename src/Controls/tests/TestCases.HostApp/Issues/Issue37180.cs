namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37180, "Label Background reset to null does not restore the default", PlatformAffected.iOS)]
public class Issue37180 : ContentPage
{
#if IOS
	const string InspectionComplete = "INSPECTION_COMPLETE:";
#endif

	readonly Label _backgroundLabel;
	readonly Label _resultLabel;

#if IOS
	readonly object _identitySentinel = new();
	object _attachedHandler;
	object _attachedLabel;
	object _attachedPlatformLabel;
	int _backgroundTransitions = -1;
	int _clickedCount = -1;
#endif
	bool _triggered;

	public Issue37180()
	{
#if IOS
		_attachedHandler = _identitySentinel;
		_attachedLabel = _identitySentinel;
		_attachedPlatformLabel = _identitySentinel;
#endif
		_backgroundLabel = new Label
		{
			Text = "Label Background Test",
			AutomationId = "BackgroundLabel",
			Padding = new Thickness(10),
		};
		_backgroundLabel.PropertyChanged += OnBackgroundLabelPropertyChanged;

		var setRedButton = new Button
		{
			Text = "Set Background to Red",
			AutomationId = "SetRedButton",
		};
		setRedButton.Clicked += OnSetBackgroundToRedClicked;

		var setNullButton = new Button
		{
			Text = "Set Background to null",
			AutomationId = "SetBackgroundButton",
		};
		setNullButton.Clicked += OnSetBackgroundToNullClicked;

		_resultLabel = new Label
		{
			Text = "WAITING_FOR_PORTRAIT",
			AutomationId = "ResultLabel",
		};

		Content = new VerticalStackLayout
		{
			Spacing = 10,
			Margin = new Thickness(20),
			Children =
			{
				_backgroundLabel,
				setRedButton,
				setNullButton,
				_resultLabel,
			},
		};

		SizeChanged += OnPageSizeChanged;
	}

	void OnPageSizeChanged(object sender, EventArgs e)
	{
		if (!_triggered && Width > 0 && Height > 0)
			_resultLabel.Text = Width < Height ? "NOT_TRIGGERED" : "WRONG_ORIENTATION";
	}

	void OnBackgroundLabelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
#if IOS
		if (e.PropertyName == VisualElement.BackgroundProperty.PropertyName)
			_backgroundTransitions++;
#endif
	}

	void OnSetBackgroundToRedClicked(object sender, EventArgs e)
	{
		_triggered = true;
#if IOS
		_clickedCount++;
		var handler = _backgroundLabel.Handler;
		if (handler?.PlatformView is not UIKit.UILabel platformLabel)
		{
			_resultLabel.Text = "RED_SETUP_FAILED";
			return;
		}

		_attachedHandler = handler;
		_attachedLabel = _backgroundLabel;
		_attachedPlatformLabel = platformLabel;
		_backgroundLabel.Background = Colors.Red;

		_resultLabel.Text =
			IsOpaqueRed(platformLabel) && _clickedCount == 0 && _backgroundTransitions == 0
				? "RED_CONFIRMED"
				: "RED_SETUP_FAILED";
#else
		_resultLabel.Text = "UNSUPPORTED_PLATFORM";
#endif
	}

	void OnSetBackgroundToNullClicked(object sender, EventArgs e)
	{
#if IOS
		_clickedCount++;
		_backgroundLabel.Background = null;

		if (!ReferenceEquals(_backgroundLabel, _attachedLabel) ||
			!ReferenceEquals(_backgroundLabel.Handler, _attachedHandler) ||
			!ReferenceEquals(_backgroundLabel.Handler?.PlatformView, _attachedPlatformLabel) ||
			_clickedCount != 1 ||
			_backgroundTransitions != 1 ||
			_backgroundLabel.Background is not null)
		{
			_resultLabel.Text = $"{InspectionComplete}SETUP_FAILED";
			return;
		}

		var platformLabel = (UIKit.UILabel)_attachedPlatformLabel;
		var result = IsTransparent(platformLabel)
			? "TRANSPARENT"
			: IsOpaqueRed(platformLabel) ? "OPAQUE_RED" : "UNEXPECTED_NATIVE_COLOR";
		_resultLabel.Text = $"{InspectionComplete}{result}";
#else
		_resultLabel.Text = "UNSUPPORTED_PLATFORM";
#endif
	}

#if IOS
	static bool IsOpaqueRed(UIKit.UILabel platformLabel)
	{
		if (platformLabel.BackgroundColor is not UIKit.UIColor backgroundColor)
			return false;

		backgroundColor.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return red > 0.99 && green < 0.01 && blue < 0.01 && alpha > 0.99;
	}

	static bool IsTransparent(UIKit.UILabel platformLabel)
	{
		if (platformLabel.BackgroundColor is not UIKit.UIColor backgroundColor)
			return true;

		backgroundColor.GetRGBA(out _, out _, out _, out var alpha);
		return alpha < 0.01;
	}
#endif
}
