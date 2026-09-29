namespace Wordzzle;

public partial class HackathonPage : ContentPage
{
	private readonly HackathonState _state = new();

	public HackathonPage()
	{
		InitializeComponent();
		BindingContext = _state;
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		_state.ClickCount += 2;
	}

	private void OnHackathonClicked(object? sender, EventArgs e)
	{
		CheckResult.Text = "Hackathon check passed: the button works.";
	}

	private void OnSubmitClicked(object? sender, EventArgs e)
	{
		_state.Submit();
	}
}
