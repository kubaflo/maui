namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36933, "DatePicker and TimePicker backgrounds are not cleared when set to null at runtime", PlatformAffected.iOS)]
public partial class Issue36933 : ContentPage
{
	bool _backgroundApplied;

	public Issue36933()
	{
		InitializeComponent();
	}

	void OnToggleBackgroundClicked(object sender, EventArgs e)
	{
		if (!_backgroundApplied)
		{
			AffectedDatePicker.Background = new SolidColorBrush(Colors.Red);
			AffectedTimePicker.Background = new SolidColorBrush(Colors.Red);
			_backgroundApplied = true;
			ToggleBackgroundButton.Text = "Clear background";
			StateLabel.Text = $"Transition 1: DatePicker Background={DescribeBackground(AffectedDatePicker.Background)}; TimePicker Background={DescribeBackground(AffectedTimePicker.Background)}";
			ResultLabel.Text = "Red backgrounds applied";
			return;
		}

		AffectedDatePicker.Background = null;
		AffectedTimePicker.Background = null;
		ToggleBackgroundButton.IsEnabled = false;
		StateLabel.Text = $"Transition 2: DatePicker Background={DescribeBackground(AffectedDatePicker.Background)}; TimePicker Background={DescribeBackground(AffectedTimePicker.Background)}";
		ResultLabel.Text = "Background properties cleared";
	}

	static string DescribeBackground(Brush background) =>
		background switch
		{
			SolidColorBrush solidColorBrush when solidColorBrush.Color == Colors.Red => "Red",
			null => "null",
			_ => background.GetType().Name,
		};
}
