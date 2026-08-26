using System.Globalization;

#if WINDOWS
using WTimePicker = Microsoft.UI.Xaml.Controls.TimePicker;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30197, "TimePicker does not update its format when culture changes at runtime", PlatformAffected.WinRT)]
public partial class Issue30197 : ContentPage
{
	public Issue30197()
	{
		SetAndVerifyCulture("en-US");
		InitializeComponent();
		ExpectedValueLabel.Text = FormatTime(CultureInfo.GetCultureInfo("fr-FR"));
	}

	void OnTimePickerLoaded(object sender, EventArgs e)
	{
#if WINDOWS
		if (CultureTimePicker.Handler?.PlatformView is not WTimePicker platformTimePicker)
		{
			LoadedStatusLabel.Text = "setup-error";
			return;
		}

		LoadedStatusLabel.Text = platformTimePicker.ClockIdentifier;
#endif
	}

	void OnChangeCultureClicked(object sender, EventArgs e)
	{
		SetAndVerifyCulture("fr-FR");
		CultureLabel.Text = $"Current culture: {CultureInfo.CurrentCulture.Name}";
		ExpectedValueLabel.Text = FormatTime(CultureInfo.CurrentCulture);

		Dispatcher.Dispatch(() => TransitionStatusLabel.Text = "post-change-complete");
	}

	string FormatTime(CultureInfo culture)
	{
		var time = CultureTimePicker.Time.GetValueOrDefault();
		return DateTime.Today.Add(time).ToString(culture.DateTimeFormat.ShortTimePattern, culture);
	}

	static void SetAndVerifyCulture(string cultureName)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;

		if (CultureInfo.CurrentCulture.Name != cultureName ||
			CultureInfo.CurrentUICulture.Name != cultureName ||
			CultureInfo.DefaultThreadCurrentCulture?.Name != cultureName ||
			CultureInfo.DefaultThreadCurrentUICulture?.Name != cultureName)
		{
			throw new InvalidOperationException($"Unable to set culture to {cultureName}.");
		}
	}
}
