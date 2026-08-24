using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 11740, "Binding does not respect Binding.DoNothing returned from IValueConverter", PlatformAffected.Android)]
public partial class Issue11740 : ContentPage
{
	public Issue11740()
	{
		InitializeComponent();

		if (Resources["DoNothingConverter"] is not Issue11740DoNothingConverter converter)
			throw new InvalidOperationException("The Binding.DoNothing converter resource was not created.");

		converter.Converted = OnConverted;
	}

	void OnApplyBindingClicked(object sender, EventArgs e)
	{
		ReproductionEntry.BindingContext = new Issue11740BindingSource();
	}

	void OnConverted(int callCount)
	{
		ResultLabel.Text = $"Converter calls: {callCount}; returned Binding.DoNothing";
	}
}

public sealed class Issue11740DoNothingConverter : IValueConverter
{
	int _convertCallCount;

	public Action<int> Converted { get; set; } = delegate { };

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		Converted(++_convertCallCount);
		return Binding.DoNothing;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		Binding.DoNothing;
}

public sealed class Issue11740BindingSource
{
	public string Value => "Source value";
}
