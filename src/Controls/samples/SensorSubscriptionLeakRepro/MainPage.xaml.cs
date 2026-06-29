using System;
using System.Threading.Tasks;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	bool _ran;

	public MainPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Autorun once shortly after the page appears so the repro produces
		// metrics with no interaction (used for headless/CI capture).
		if (_ran)
			return;
		_ran = true;

		await Task.Delay(500);
		RunAndShow();
	}

	void OnRunClicked(object sender, EventArgs e) => RunAndShow();

	void RunAndShow()
	{
		ResultsLabel.Text = "Running…";
		var report = LeakHarness.RunAll();
		ResultsLabel.Text = report;
	}
}
