using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30090, "DatePicker does not update its format when the culture changes at runtime", PlatformAffected.UWP)]
public partial class Issue30090 : ContentPage
{
	static readonly CultureInfo EnglishCulture = new("en-US");
	static readonly CultureInfo FrenchCulture = new("fr-FR");

	public Issue30090()
	{
		SetCulture(EnglishCulture);
		InitializeComponent();
		InitialCultureLabel.Text = $"Initial culture: {CultureInfo.CurrentCulture.Name}";
	}

	void OnChangeCultureClicked(object sender, EventArgs e)
	{
		SetCulture(FrenchCulture);
		CultureStatusLabel.Text = $"Current culture: {CultureInfo.CurrentCulture.Name}";
	}

	static void SetCulture(CultureInfo culture)
	{
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}
}
