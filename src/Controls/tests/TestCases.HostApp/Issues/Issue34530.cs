using System.Globalization;
using Microsoft.Maui.Media;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34530, "TextToSpeech GetLocalesAsync does not return Lithuanian on iOS", PlatformAffected.iOS)]
public class Issue34530 : ContentPage
{
	readonly Label _localeQueryStatusLabel;
	readonly Label _pickerFocusStatusLabel;
	readonly Label _totalLocaleCountLabel;
	readonly Label _lithuanianLocaleCountLabel;
	readonly Picker _localePicker;
	bool _localesLoaded;

	public Issue34530()
	{
		_localeQueryStatusLabel = new Label
		{
			AutomationId = "LocaleQueryStatusLabel",
			Text = "Locale query not started"
		};

		_pickerFocusStatusLabel = new Label
		{
			AutomationId = "PickerFocusStatusLabel",
			Text = "Picker not focused"
		};

		_totalLocaleCountLabel = new Label
		{
			AutomationId = "TotalLocaleCountLabel",
			Text = "-1"
		};

		_lithuanianLocaleCountLabel = new Label
		{
			AutomationId = "LithuanianLocaleCountLabel",
			Text = "-1"
		};

		_localePicker = new Picker
		{
			AutomationId = "LocalePicker",
			Title = "Select a locale"
		};
		_localePicker.Focused += OnLocalePickerFocused;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "iOS text-to-speech locales",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				},
				new Label
				{
					Text = "Open the picker to load and inspect the languages returned by TextToSpeech.Default.GetLocalesAsync()."
				},
				_localeQueryStatusLabel,
				_pickerFocusStatusLabel,
				new Label { Text = "Total locale count:" },
				_totalLocaleCountLabel,
				new Label { Text = "Lithuanian locale count:" },
				_lithuanianLocaleCountLabel,
				_localePicker
			}
		};
	}

	async void OnLocalePickerFocused(object sender, FocusEventArgs e)
	{
		if (_localesLoaded)
			return;

		_localesLoaded = true;
		_pickerFocusStatusLabel.Text = "Picker focused";
		_localeQueryStatusLabel.Text = "Loading locales";

		var locales = (await TextToSpeech.Default.GetLocalesAsync())
			.OrderBy(locale => locale.Language, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		var lithuanianLocaleCount = locales.Count(locale =>
			string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
			(locale.Language is string language && language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase)));

		_localePicker.ItemsSource = locales
			.Select(locale => $"{locale.Language} - {locale.Name}")
			.ToArray();
		_totalLocaleCountLabel.Text = locales.Length.ToString(CultureInfo.InvariantCulture);
		_lithuanianLocaleCountLabel.Text = lithuanianLocaleCount.ToString(CultureInfo.InvariantCulture);
		_localeQueryStatusLabel.Text = "Locale query completed";
	}
}

