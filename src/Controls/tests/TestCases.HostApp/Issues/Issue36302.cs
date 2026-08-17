namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36302, "Image and ImageButton BackgroundColor does not reset when set to null", PlatformAffected.iOS)]
public class Issue36302 : ContentPage
{
	readonly Image _issueImage;
	readonly ImageButton _issueImageButton;
	readonly Label _stateLabel;
	readonly Label _resultLabel;

	public Issue36302()
	{
		Title = "Issue 36302";
		AutomationId = "Issue36302Page";

		_issueImage = new Image
		{
			AutomationId = "IssueImage",
			BackgroundColor = Colors.Blue,
			HeightRequest = 120,
			Source = "dotnet_bot.png",
			WidthRequest = 120
		};

		_issueImageButton = new ImageButton
		{
			AutomationId = "IssueImageButton",
			BackgroundColor = Colors.Blue,
			HeightRequest = 120,
			Source = "dotnet_bot.png",
			WidthRequest = 120
		};

		_stateLabel = new Label
		{
			AutomationId = "StateLabel",
			Text = "Current BackgroundColor: Blue"
		};

		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "PENDING: 0"
		};

		var setRedButton = new Button
		{
			AutomationId = "SetRedButton",
			Text = "Set backgrounds red"
		};
		setRedButton.Clicked += OnSetRedClicked;

		var clearButton = new Button
		{
			AutomationId = "ClearButton",
			Text = "Clear backgrounds to null"
		};
		clearButton.Clicked += OnClearClicked;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Image backgrounds before and after clearing"
					},
					new Label { Text = "Image" },
					_issueImage,
					new Label { Text = "ImageButton" },
					_issueImageButton,
					_stateLabel,
					setRedButton,
					clearButton,
					_resultLabel
				}
			}
		};
	}

	void OnSetRedClicked(object sender, EventArgs e)
	{
		_issueImage.BackgroundColor = Colors.Red;
		_issueImageButton.BackgroundColor = Colors.Red;
		_stateLabel.Text = "Current BackgroundColor: Red";

#if IOS
		bool managedBackgroundsRed =
			_issueImage.BackgroundColor == Colors.Red &&
			_issueImageButton.BackgroundColor == Colors.Red;
		bool nativeBackgroundsRed =
			HasNativeBackgroundColor(_issueImage, UIKit.UIColor.Red) &&
			HasNativeBackgroundColor(_issueImageButton, UIKit.UIColor.Red);

		_resultLabel.Text = managedBackgroundsRed && nativeBackgroundsRed
			? "RED: 1"
			: "RED NOT APPLIED: 1";
#else
		_resultLabel.Text = "UNSUPPORTED: 1";
#endif
	}

	void OnClearClicked(object sender, EventArgs e)
	{
		_issueImage.BackgroundColor = null;
		_issueImageButton.BackgroundColor = null;
		_stateLabel.Text = "Current BackgroundColor: null";

#if IOS
		bool managedBackgroundsCleared =
			_issueImage.BackgroundColor is null &&
			_issueImageButton.BackgroundColor is null;
		bool nativeBackgroundsCleared =
			HasTransparentNativeBackground(_issueImage) &&
			HasTransparentNativeBackground(_issueImageButton);

		_resultLabel.Text = managedBackgroundsCleared && nativeBackgroundsCleared
			? "CLEARED: 2"
			: "RETAINED: 2";
#else
		_resultLabel.Text = "UNSUPPORTED: 2";
#endif
	}

#if IOS
	static bool HasNativeBackgroundColor(VisualElement element, UIKit.UIColor expectedColor)
	{
		if (element.Handler?.PlatformView is not UIKit.UIView nativeView ||
			nativeView.BackgroundColor is not UIKit.UIColor nativeColor)
		{
			return false;
		}

		nativeColor.GetRGBA(out nfloat nativeRed, out nfloat nativeGreen, out nfloat nativeBlue, out nfloat nativeAlpha);
		expectedColor.GetRGBA(out nfloat expectedRed, out nfloat expectedGreen, out nfloat expectedBlue, out nfloat expectedAlpha);

		return Math.Abs((double)(nativeRed - expectedRed)) < 0.001 &&
			Math.Abs((double)(nativeGreen - expectedGreen)) < 0.001 &&
			Math.Abs((double)(nativeBlue - expectedBlue)) < 0.001 &&
			Math.Abs((double)(nativeAlpha - expectedAlpha)) < 0.001;
	}

	static bool HasTransparentNativeBackground(VisualElement element) =>
		element.Handler?.PlatformView is UIKit.UIView nativeView &&
		(nativeView.BackgroundColor is null || nativeView.BackgroundColor.CGColor.Alpha <= 0);
#endif
}
