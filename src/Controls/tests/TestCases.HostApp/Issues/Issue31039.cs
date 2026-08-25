#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31039, "Entry gains focus when an InputTransparent Entry is clicked inside a ScrollView", PlatformAffected.UWP)]
public class Issue31039 : ContentPage
{
	readonly Entry _firstEntry;
	readonly Label _probeLabel;
	int _tapSequence = -1;
	int _initialFirstEntryFocused = -1;
	int _postClickFirstEntryFocused = -1;
	int _focusEventCount = -1;

	public Issue31039()
	{
		var contentLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16
		};

		contentLayout.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(OnContentTapped)
		});

		contentLayout.Children.Add(new Label
		{
			Text = "Click the input-transparent Entry below."
		});

		_firstEntry = new Entry
		{
			AutomationId = "FirstEntry",
			Placeholder = "First focus-enabled Entry"
		};
		_firstEntry.Focused += OnFirstEntryFocused;
		contentLayout.Children.Add(_firstEntry);

		contentLayout.Children.Add(new Entry
		{
			AutomationId = "TransparentEntry",
			InputTransparent = true,
			Text = "Click this InputTransparent Entry"
		});

		contentLayout.Children.Add(new Entry
		{
			AutomationId = "ThirdEntry",
			Placeholder = "Third focus-enabled Entry"
		});

		_probeLabel = new Label
		{
			AutomationId = "ProbeLabel"
		};
		PublishProbe();
		contentLayout.Children.Add(_probeLabel);

		Content = new ScrollView
		{
			Content = contentLayout
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		Dispatcher.Dispatch(() =>
		{
			_initialFirstEntryFocused = _firstEntry.IsFocused ? 1 : 0;
			_focusEventCount = Math.Max(_focusEventCount, 0);
			PublishProbe();
		});
	}

	void OnContentTapped()
	{
		_tapSequence = 0;
		PublishProbe();
		Dispatcher.Dispatch(() =>
		{
			_postClickFirstEntryFocused = _firstEntry.IsFocused ? 1 : 0;
			_tapSequence = 1;
			PublishProbe();
		});
	}

	void OnFirstEntryFocused(object sender, FocusEventArgs e)
	{
		_focusEventCount = 1;
		PublishProbe();
	}

	void PublishProbe()
	{
		_probeLabel.Text = $"InputTransparent=True; TapSequence={_tapSequence}; InitialFirstEntryFocused={_initialFirstEntryFocused}; PostClickFirstEntryFocused={_postClickFirstEntryFocused}; FocusEventCount={_focusEventCount}";
	}
}
#endif

