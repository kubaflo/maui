#if IOS
using System.Globalization;
using Microsoft.Maui.Handlers;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37180, "Label Background does not reset to the transparent default when set to null", PlatformAffected.iOS)]
public class Issue37180 : ContentPage
{
	readonly Label _instructionLabel;
	readonly Label _affectedLabel;
	readonly Label _measurementLabel;

	double _instructionAlpha = -1;
	double _initialAlpha = -1;
	double _redAlpha = -1;
	double _nullAlpha = -1;
	string _nullRgba = "unavailable";
	int _transitionToken = -1;
	bool _managedBackgroundIsNull;

	public Issue37180()
	{
		_instructionLabel = new Label
		{
			Text = "Set the Label background to red, then clear it to null."
		};

		_affectedLabel = new Label
		{
			AutomationId = "BackgroundLabel",
			Padding = 10,
			Text = "Label Background Test"
		};

		var setRedButton = new Button
		{
			AutomationId = "SetRedButton",
			Text = "Set Background to Red"
		};
		setRedButton.Clicked += OnSetRedClicked;

		var setNullButton = new Button
		{
			AutomationId = "SetNullButton",
			Text = "Set Background to null"
		};
		setNullButton.Clicked += OnSetNullClicked;

		_measurementLabel = new Label
		{
			AutomationId = "MeasurementLabel",
			FontAttributes = FontAttributes.Bold
		};

		Content = new VerticalStackLayout
		{
			Margin = 20,
			Spacing = 10,
			Children =
			{
				_instructionLabel,
				_affectedLabel,
				setRedButton,
				setNullButton,
				_measurementLabel
			}
		};

		Loaded += OnLoaded;
		UpdateMeasurement();
	}

	void OnLoaded(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(() => CaptureInitialState(3));
	}

	void CaptureInitialState(int attemptsRemaining)
	{
		var instructionSampled = TrySampleNativeColor(_instructionLabel, out _, out _, out _, out _instructionAlpha);
		var affectedSampled = TrySampleNativeColor(_affectedLabel, out _, out _, out _, out _initialAlpha);

		if ((!instructionSampled || !affectedSampled) && attemptsRemaining > 1)
		{
			Dispatcher.Dispatch(() => CaptureInitialState(attemptsRemaining - 1));
			return;
		}

		UpdateMeasurement();
	}

	void OnSetRedClicked(object sender, EventArgs e)
	{
		_affectedLabel.Background = Colors.Red;
		Dispatcher.Dispatch(() => CaptureRedState(3));
	}

	void CaptureRedState(int attemptsRemaining)
	{
		var sampled = TrySampleNativeColor(_affectedLabel, out _, out _, out _, out _redAlpha);
		if ((!sampled || Math.Abs(_redAlpha - 1) > 0.001) && attemptsRemaining > 1)
		{
			Dispatcher.Dispatch(() => CaptureRedState(attemptsRemaining - 1));
			return;
		}

		_transitionToken = 1;
		UpdateMeasurement();
	}

	void OnSetNullClicked(object sender, EventArgs e)
	{
		_affectedLabel.Background = null;
		_managedBackgroundIsNull = _affectedLabel.Background is null;
		Dispatcher.Dispatch(() => CaptureNullState(3));
	}

	void CaptureNullState(int attemptsRemaining)
	{
		var sampled = TrySampleNativeColor(_affectedLabel, out var red, out var green, out var blue, out _nullAlpha);
		if ((!sampled || Math.Abs(_nullAlpha) > 0.001) && attemptsRemaining > 1)
		{
			Dispatcher.Dispatch(() => CaptureNullState(attemptsRemaining - 1));
			return;
		}

		_nullRgba = FormattableString.Invariant($"{red:F3},{green:F3},{blue:F3},{_nullAlpha:F3}");
		_transitionToken = 2;
		UpdateMeasurement();
	}

	void UpdateMeasurement()
	{
		_measurementLabel.Text = string.Format(
			CultureInfo.InvariantCulture,
			"Transition={0};Instruction={1:F3};Initial={2:F3};Red={3:F3};Null={4:F3};ManagedNull={5};NullRGBA={6}",
			_transitionToken,
			_instructionAlpha,
			_initialAlpha,
			_redAlpha,
			_nullAlpha,
			_managedBackgroundIsNull,
			_nullRgba);
	}

	static bool TrySampleNativeColor(Label label, out double red, out double green, out double blue, out double alpha)
	{
		red = green = blue = alpha = -1;

		if (label.Handler is not ILabelHandler labelHandler)
			return false;

		var nativeColor = labelHandler.PlatformView.BackgroundColor;
		if (nativeColor is null)
		{
			red = green = blue = alpha = 0;
			return true;
		}

		nativeColor.GetRGBA(out var nativeRed, out var nativeGreen, out var nativeBlue, out var nativeAlpha);
		red = (double)nativeRed;
		green = (double)nativeGreen;
		blue = (double)nativeBlue;
		alpha = (double)nativeAlpha;
		return true;
	}
}
#endif

