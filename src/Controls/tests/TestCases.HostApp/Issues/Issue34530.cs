#if IOS
using Microsoft.Maui.Media;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34530, "TextToSpeech.Default.GetLocalesAsync does not return Lithuanian on iOS", PlatformAffected.iOS)]
public class Issue34530 : ContentPage
{
	bool _localesLoaded;

	public Issue34530()
	{
		Title = "TextToSpeech locales";

		var localePicker = new Picker
		{
			AutomationId = "LocalePicker",
			Title = "Select a returned locale"
		};

		var loadStateEntry = new Entry
		{
			Text = "Locale query pending",
			IsReadOnly = true,
			FontAttributes = FontAttributes.Bold
		};

		var localeSummaryEditor = new Editor
		{
			AutomationId = "LocaleSummary",
			Text = "Returned locales will appear here.",
			IsReadOnly = true,
			AutoSize = EditorAutoSizeOption.TextChanges,
			MinimumHeightRequest = 240
		};

		localePicker.Focused += async (sender, args) =>
		{
			if (_localesLoaded)
				return;

			_localesLoaded = true;
			var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToArray();
			var localeDescriptions = locales
				.Select(locale => $"{locale.Language} - {locale.Name}")
				.ToArray();

			localePicker.ItemsSource = localeDescriptions;
			localeSummaryEditor.Text = $"Returned locales ({locales.Length}):\n{string.Join("\n", localeDescriptions)}";
			loadStateEntry.Text = $"Locale query completed with {locales.Length} results";
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
						Text = "TextToSpeech locale list",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Tap the picker to load the locales returned by TextToSpeech.Default.GetLocalesAsync()."
					},
					localePicker,
					loadStateEntry,
					localeSummaryEditor
				}
			}
		};
	}
}
#endif

