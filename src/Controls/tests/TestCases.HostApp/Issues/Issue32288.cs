using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32288, "Keyboard Numeric is not working in iOS", PlatformAffected.iOS)]
public class Issue32288 : ContentPage
{
	public Issue32288()
	{
		var culture = CultureInfo.GetCultureInfo("en-US");
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.CurrentCulture = culture;

		var application = Application.Current ?? throw new InvalidOperationException("The application must be initialized.");
		application.UserAppTheme = AppTheme.Light;

		var focusStatus = new Label
		{
			AutomationId = "FocusStatus",
			FontSize = 18,
			Text = "-1"
		};

		var numericEntry = new Entry
		{
			AutomationId = "NumericEntry",
			Keyboard = Keyboard.Numeric,
			Placeholder = "Numeric value"
		};

		numericEntry.Focused += (_, _) => focusStatus.Text = "1";

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					AutomationId = "EnvironmentStatus",
					FontSize = 18,
					Text = $"Tap the numeric Entry and inspect its signed decimal keys.|{CultureInfo.CurrentCulture.Name}|{culture.NumberFormat.NumberDecimalSeparator}|{culture.NumberFormat.NegativeSign}|{application.UserAppTheme}"
				},
				numericEntry,
				focusStatus
			}
		};
	}
}

