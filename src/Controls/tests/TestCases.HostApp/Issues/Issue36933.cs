namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36933, "DatePicker and TimePicker Background is not cleared when set to null at runtime", PlatformAffected.iOS)]
public class Issue36933 : ContentPage
{
	readonly DatePicker _affectedDatePicker;
	readonly TimePicker _affectedTimePicker;
	readonly Label _stateLabel;
	readonly Label _resultLabel;

	bool _defaultsCaptured;
	int _clickCount;

	object _datePickerDefaultBackground = new();
	object _timePickerDefaultBackground = new();
	object _datePickerAppliedBackground = new();
	object _timePickerAppliedBackground = new();

	public Issue36933()
	{
		_affectedDatePicker = new DatePicker
		{
			AutomationId = "AffectedDatePicker",
		};

		_affectedTimePicker = new TimePicker
		{
			AutomationId = "AffectedTimePicker",
		};

		var toggleBackgroundButton = new Button
		{
			AutomationId = "ToggleBackgroundButton",
			Text = "Toggle picker backgrounds",
		};
		toggleBackgroundButton.Clicked += OnToggleBackgroundClicked;

		_stateLabel = new Label
		{
			AutomationId = "StateLabel",
			Text = "Waiting for attached platform-default picker backgrounds.",
		};

		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "DatePicker: NOT RUN; TimePicker: NOT RUN",
		};

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
						AutomationId = "IssueTitle",
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "DatePicker and TimePicker background clearing",
					},
					new Label
					{
						Text = "The first tap applies gold backgrounds. The second tap clears Background to null; both pickers should return to their original platform-default appearance.",
					},
					_affectedDatePicker,
					_affectedTimePicker,
					toggleBackgroundButton,
					_stateLabel,
					_resultLabel,
				},
			},
		};

		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		PollUntil(CaptureNativeDefaults, captured =>
		{
			_defaultsCaptured = captured;
			_stateLabel.Text = captured
				? "Ready: attached platform-default picker backgrounds captured."
				: "Setup failed: picker platform views did not attach to native windows.";
		});
	}

	void OnToggleBackgroundClicked(object sender, EventArgs e)
	{
		if (!_defaultsCaptured)
		{
			_stateLabel.Text = "Setup failed: native defaults were not captured.";
			return;
		}

		_clickCount++;
		if (_clickCount == 1)
		{
			_affectedDatePicker.Background = Colors.Gold;
			_affectedTimePicker.Background = Colors.Gold;

			PollUntil(AreNativeBackgroundsGold, applied =>
			{
				_stateLabel.Text = applied
					? "Gold backgrounds applied natively."
					: "Setup failed: gold backgrounds were not applied natively.";
			});
			return;
		}

		_resultLabel.Text = "DatePicker: NOT RUN; TimePicker: NOT RUN";
		_affectedDatePicker.Background = null;
		_affectedTimePicker.Background = null;

		PollUntil(AreNativeBackgroundsCleared, _ =>
		{
			var datePickerState = GetDatePickerState();
			var timePickerState = GetTimePickerState();

			_resultLabel.Text = $"DatePicker: {datePickerState}; TimePicker: {timePickerState}";
			_stateLabel.Text = $"Native check complete: click count {_clickCount}; managed backgrounds null: {_affectedDatePicker.Background is null && _affectedTimePicker.Background is null}.";
		});
	}

	void PollUntil(Func<bool> condition, Action<bool> completed, int attemptsRemaining = 50)
	{
		if (condition())
		{
			completed(true);
			return;
		}

		if (attemptsRemaining == 0)
		{
			completed(false);
			return;
		}

		Dispatcher.Dispatch(() => PollUntil(condition, completed, attemptsRemaining - 1));
	}

	bool CaptureNativeDefaults()
	{
		return TryGetAttachedNativeBackground(_affectedDatePicker, out _datePickerDefaultBackground) &&
			TryGetAttachedNativeBackground(_affectedTimePicker, out _timePickerDefaultBackground);
	}

	bool AreNativeBackgroundsGold()
	{
		if (!TryGetAttachedNativeBackground(_affectedDatePicker, out var dateBackground) ||
			!TryGetAttachedNativeBackground(_affectedTimePicker, out var timeBackground) ||
			NativeValuesEqual(dateBackground, _datePickerDefaultBackground) ||
			NativeValuesEqual(timeBackground, _timePickerDefaultBackground))
		{
			return false;
		}

		_datePickerAppliedBackground = dateBackground;
		_timePickerAppliedBackground = timeBackground;
		return true;
	}

	bool AreNativeBackgroundsCleared()
	{
		return TryGetAttachedNativeBackground(_affectedDatePicker, out var dateBackground) &&
			TryGetAttachedNativeBackground(_affectedTimePicker, out var timeBackground) &&
			NativeValuesEqual(dateBackground, _datePickerDefaultBackground) &&
			NativeValuesEqual(timeBackground, _timePickerDefaultBackground);
	}

	string GetDatePickerState()
	{
		return TryGetAttachedNativeBackground(_affectedDatePicker, out var background)
			? GetNativeState(background, _datePickerDefaultBackground, _datePickerAppliedBackground)
			: "UNAVAILABLE";
	}

	string GetTimePickerState()
	{
		return TryGetAttachedNativeBackground(_affectedTimePicker, out var background)
			? GetNativeState(background, _timePickerDefaultBackground, _timePickerAppliedBackground)
			: "UNAVAILABLE";
	}

	static bool TryGetAttachedNativeBackground(VisualElement picker, out object background)
	{
		background = null;
#if IOS
		if (picker.Handler?.PlatformView is not UIKit.UIView platformView ||
			platformView.Window is null)
		{
			return false;
		}

		background = platformView.BackgroundColor;
		return true;
#else
		return false;
#endif
	}

	static string GetNativeState(object actual, object platformDefault, object applied)
	{
		if (NativeValuesEqual(actual, platformDefault))
			return "CLEARED";

		return NativeValuesEqual(actual, applied) ? "RETAINED" : "MISMATCH";
	}

	static bool NativeValuesEqual(object first, object second) => Equals(first, second);
}
