namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "Border with Rotation and HorizontalOptions.Start is positioned incorrectly on initial load", PlatformAffected.Android)]
public partial class Issue33530 : ContentPage
{
	readonly bool _isRotatedModal;

	public Issue33530()
		: this(false)
	{
	}

	Issue33530(bool isRotatedModal)
	{
		_isRotatedModal = isRotatedModal;
		InitializeComponent();

		if (_isRotatedModal)
		{
			ModeLabel.Text = "Modal reproduction loaded";
			OpenButton.IsVisible = false;
			RotatedBorder.Loaded += OnRotatedBorderLoaded;
		}
		else
		{
			RotatedBorder.Rotation = 0;
			RotatedBorder.HorizontalOptions = LayoutOptions.Center;
		}
	}

	async void OnOpenClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new Issue33530(true), false);
	}

	void OnRotatedBorderLoaded(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(() =>
		{
			if (_isRotatedModal && RotatedBorder.Width > 0 && RotatedBorder.Height > 0)
				LifecycleLabel.Text = "1";
		});
	}
}
