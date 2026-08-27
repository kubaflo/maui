#if WINDOWS
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30090, "DatePicker does not update its format when the culture changes at runtime", PlatformAffected.UWP)]
public class Issue30090 : ContentPage
{
	public Issue30090()
	{
		var initialCulture = CultureInfo.GetCultureInfo("en-US");
		CultureInfo.CurrentCulture = initialCulture;
		CultureInfo.CurrentUICulture = initialCulture;
		CultureInfo.DefaultThreadCurrentCulture = initialCulture;
		CultureInfo.DefaultThreadCurrentUICulture = initialCulture;

		var activeCultureLabel = new Label
		{
			AutomationId = "ActiveCultureLabel",
			Text = $"Active culture: {CultureInfo.CurrentCulture.Name}"
		};

		var callbackMarkerLabel = new Label
		{
			AutomationId = "CultureCallbackMarkerLabel",
			Text = "Culture callback marker: -1"
		};

		var datePicker = new DatePicker
		{
			AutomationId = "DatePickerControl",
			Date = new DateTime(2025, 12, 31)
		};

		var changeCultureButton = new Button
		{
			AutomationId = "ChangeCultureButton",
			Text = "Change culture to fr-FR"
		};

		var resultLabel = new Label
		{
			AutomationId = "CultureChangeResultLabel",
			Text = "Culture has not changed"
		};

		changeCultureButton.Clicked += (_, _) =>
		{
			var frenchCulture = CultureInfo.GetCultureInfo("fr-FR");
			CultureInfo.CurrentCulture = frenchCulture;
			CultureInfo.CurrentUICulture = frenchCulture;
			CultureInfo.DefaultThreadCurrentCulture = frenchCulture;
			CultureInfo.DefaultThreadCurrentUICulture = frenchCulture;

			activeCultureLabel.Text = $"Active culture: {CultureInfo.CurrentCulture.Name}";
			callbackMarkerLabel.Text = "Culture callback marker: 1";
			resultLabel.Text = "Culture changed without modifying the DatePicker";
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 30,
				Spacing = 18,
				Children =
				{
					new Label
					{
						Text = "DatePicker runtime culture change",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "The selected date starts under en-US. Change the culture without changing the date."
					},
					datePicker,
					activeCultureLabel,
					new Label
					{
						AutomationId = "ExpectedRenderedLabel",
						Text = "Expected fr-FR digits: 31122025"
					},
					changeCultureButton,
					callbackMarkerLabel,
					resultLabel
				}
			}
		};
	}
}
#endif

