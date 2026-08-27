using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30090, "DatePicker does not update its format when culture changes at runtime", PlatformAffected.UWP)]
public class Issue30090 : ContentPage
{
	readonly CultureInfo _previousCurrentCulture;
	readonly CultureInfo _previousCurrentUICulture;
	readonly CultureInfo _previousDefaultThreadCulture;
	readonly CultureInfo _previousDefaultThreadUICulture;
	readonly bool _hadDefaultThreadCulture;
	readonly bool _hadDefaultThreadUICulture;
	bool _culturesRestored;

	public Issue30090()
	{
		_previousCurrentCulture = CultureInfo.CurrentCulture;
		_previousCurrentUICulture = CultureInfo.CurrentUICulture;

		var previousDefaultThreadCulture = CultureInfo.DefaultThreadCurrentCulture;
		_hadDefaultThreadCulture = previousDefaultThreadCulture is not null;
		_previousDefaultThreadCulture = previousDefaultThreadCulture ?? CultureInfo.InvariantCulture;

		var previousDefaultThreadUICulture = CultureInfo.DefaultThreadCurrentUICulture;
		_hadDefaultThreadUICulture = previousDefaultThreadUICulture is not null;
		_previousDefaultThreadUICulture = previousDefaultThreadUICulture ?? CultureInfo.InvariantCulture;

		var initialCulture = new CultureInfo("en-US");
		SetCulture(initialCulture);

		var testDate = new DateTime(2025, 12, 24);
		var datePicker = new DatePicker
		{
			AutomationId = "AffectedDatePicker",
			Date = testDate,
			Format = "d"
		};

		var cultureStatusLabel = new Label
		{
			AutomationId = "CultureStatusLabel",
			Text = GetCultureStatus()
		};

		var managedFormatLabel = new Label
		{
			AutomationId = "ManagedFormatLabel",
			Text = $"Managed en-US format: {testDate.ToString("d", initialCulture)}"
		};

		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "Observation pending"
		};

		var recordObservationButton = new Button
		{
			AutomationId = "RecordObservationButton",
			IsEnabled = false,
			Text = "Record observed format"
		};
		recordObservationButton.Clicked += (sender, args) => resultLabel.Text = "Culture change recorded";

		var changeCultureButton = new Button
		{
			AutomationId = "ChangeCultureButton",
			Text = "Change culture to fr-FR"
		};
		changeCultureButton.Clicked += (sender, args) =>
		{
			var targetCulture = new CultureInfo("fr-FR");
			SetCulture(targetCulture);
			cultureStatusLabel.Text = GetCultureStatus();
			managedFormatLabel.Text = $"Managed fr-FR format: {testDate.ToString("d", targetCulture)}";
			recordObservationButton.IsEnabled = true;
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontSize = 24,
						Text = "DatePicker runtime culture change"
					},
					cultureStatusLabel,
					datePicker,
					managedFormatLabel,
					changeCultureButton,
					recordObservationButton,
					resultLabel
				}
			}
		};
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		if (_culturesRestored)
			return;

		CultureInfo.CurrentCulture = _previousCurrentCulture;
		CultureInfo.CurrentUICulture = _previousCurrentUICulture;
		CultureInfo.DefaultThreadCurrentCulture = _hadDefaultThreadCulture ? _previousDefaultThreadCulture : null;
		CultureInfo.DefaultThreadCurrentUICulture = _hadDefaultThreadUICulture ? _previousDefaultThreadUICulture : null;
		_culturesRestored = true;
	}

	static string GetCultureStatus() =>
		$"Current={CultureInfo.CurrentCulture.Name}; UI={CultureInfo.CurrentUICulture.Name}; " +
		$"Default={CultureInfo.DefaultThreadCurrentCulture?.Name ?? "<null>"}; " +
		$"DefaultUI={CultureInfo.DefaultThreadCurrentUICulture?.Name ?? "<null>"}";

	static void SetCulture(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}
}

