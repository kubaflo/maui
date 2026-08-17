namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37323, "ScrollView padding does not update after a bound runtime change", PlatformAffected.Android)]
public partial class Issue37323 : ContentPage
{
	Thickness _dynamicPadding;

	public Issue37323()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public Thickness DynamicPadding
	{
		get => _dynamicPadding;
		set
		{
			if (_dynamicPadding == value)
				return;

			_dynamicPadding = value;
			OnPropertyChanged();
		}
	}

	void OnApplyPaddingClicked(object sender, EventArgs e)
	{
		DynamicPadding = new Thickness(48);
		SemanticProperties.SetDescription(TransitionStatus, "ApplyCompleted");
	}

	void OnCheckPaddingClicked(object sender, EventArgs e)
	{
		SemanticProperties.SetDescription(TransitionStatus, "CheckCompleted");
	}
}
