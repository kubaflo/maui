using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29956, "[Android] ImageButton border is incomplete with AspectFill", PlatformAffected.Android)]
public partial class Issue29956 : ContentPage
{
	int _measurementGeneration = -1;
	bool _initialMeasurementQueued;
	bool _imageLoadStarted;
	bool _imageLoadCompleted;

	public Issue29956()
	{
		InitializeComponent();

		AffectedImageButton.Loaded += OnImageButtonLoaded;
		AffectedImageButton.PropertyChanged += OnImageButtonPropertyChanged;
	}

	void OnImageButtonLoaded(object sender, EventArgs e) => QueueInitialMeasurement();

	void OnImageButtonPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ImageButton.IsLoading))
			return;

		if (AffectedImageButton.IsLoading)
		{
			_imageLoadStarted = true;
			return;
		}

		_imageLoadCompleted = _imageLoadStarted;
		QueueInitialMeasurement();
	}

	void QueueInitialMeasurement()
	{
		if (_initialMeasurementQueued || !_imageLoadCompleted || !AffectedImageButton.IsLoaded || AffectedImageButton.IsLoading)
			return;

		_initialMeasurementQueued = true;
		QueueNativeMeasurement("AspectFit", InitialMeasurementLabel);
	}

	void OnApplyAspectFillClicked(object sender, EventArgs e)
	{
		AffectedImageButton.Aspect = Aspect.AspectFill;
		CurrentAspectLabel.Text = "Current aspect: AspectFill";
		ResultLabel.Text = "AspectFill applied";
		QueueNativeMeasurement("AspectFill", PostMeasurementLabel);
	}

	partial void QueueNativeMeasurement(string phase, Label targetLabel);
}
