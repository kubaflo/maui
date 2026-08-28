using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;

namespace Microsoft.Maui.Media
{
	partial class TextToSpeechImplementation : ITextToSpeech
	{
#pragma warning disable CA1416 // https://github.com/xamarin/xamarin-macios/issues/14619
		readonly Lazy<AVSpeechSynthesizer> speechSynthesizer = new(() => new AVSpeechSynthesizer());

		Task<IEnumerable<Locale>> PlatformGetLocalesAsync()
		{
			var locales = new List<Locale>();
			var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var voice in AVSpeechSynthesisVoice.GetSpeechVoices())
			{
				locales.Add(new Locale(voice.Language, null, voice.Name, voice.Identifier));

				var language = GetLanguageSubtag(voice.Language);
				if (language.Length > 0)
					languages.Add(language);
			}

			// GetSpeechVoices() only enumerates voices whose assets are installed on the device, so a
			// language the user never downloaded is invisible even though it can still be requested.
			// Report the languages that the managed culture data and the platform both recognize.
			var platformLanguages = new HashSet<string>(NSLocale.ISOLanguageCodes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

			foreach (var culture in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
			{
				var language = culture.Name;
				if (language.Length == 0 || language.IndexOfAny(LanguageSubtagSeparators) >= 0)
					continue;

				if (!platformLanguages.Contains(language) || !languages.Add(language))
					continue;

				locales.Add(new Locale(language, null, culture.DisplayName, null));
			}

			return Task.FromResult<IEnumerable<Locale>>(locales);
		}

		static readonly char[] LanguageSubtagSeparators = new[] { '-', '_' };

		static string GetLanguageSubtag(string languageTag)
		{
			if (string.IsNullOrWhiteSpace(languageTag))
				return string.Empty;

			var separator = languageTag.IndexOfAny(LanguageSubtagSeparators);
			return separator < 0 ? languageTag : languageTag.Substring(0, separator);
		}

		async Task PlatformSpeakAsync(string text, SpeechOptions options, CancellationToken cancelToken)
		{
			using var speechUtterance = GetSpeechUtterance(text, options);
			await SpeakUtterance(speechUtterance, cancelToken);
		}

		static AVSpeechUtterance GetSpeechUtterance(string text, SpeechOptions options)
		{
			var speechUtterance = new AVSpeechUtterance(text);

			if (options != null)
			{
				// null voice if fine - it is the default
				// select the voice by identifier else by Language, otherwise set for default
				speechUtterance.Voice =
				    options.Locale?.Id != null
				        ? AVSpeechSynthesisVoice.FromIdentifier(options.Locale.Id)
				        : AVSpeechSynthesisVoice.FromLanguage(options.Locale?.Language)
				        ?? AVSpeechSynthesisVoice.FromLanguage(AVSpeechSynthesisVoice.CurrentLanguageCode);

				// the platform has a range of 0.5 - 2.0
				// anything lower than 0.5 is set to 0.5
				if (options.Pitch.HasValue)
					speechUtterance.PitchMultiplier = options.Pitch.Value;

				if (options.Volume.HasValue)
					speechUtterance.Volume = options.Volume.Value;

				if (options.Rate.HasValue)
					speechUtterance.Rate = NormalizeRate(options.Rate.Value);
			}

			return speechUtterance;
		}

		async Task SpeakUtterance(AVSpeechUtterance speechUtterance, CancellationToken cancelToken)
		{
			var tcsUtterance = new TaskCompletionSource<bool>();
			try
			{
				speechSynthesizer.Value.DidFinishSpeechUtterance += OnFinishedSpeechUtterance;
				speechSynthesizer.Value.SpeakUtterance(speechUtterance);
				using (cancelToken.Register(TryCancel))
				{
					await tcsUtterance.Task;
				}
			}
			finally
			{
				speechSynthesizer.Value.DidFinishSpeechUtterance -= OnFinishedSpeechUtterance;
			}

			void TryCancel()
			{
				speechSynthesizer.Value?.StopSpeaking(AVSpeechBoundary.Immediate);
				tcsUtterance?.TrySetResult(true);
			}

			void OnFinishedSpeechUtterance(object sender, AVSpeechSynthesizerUteranceEventArgs args)
			{
				if (speechUtterance == args.Utterance)
					tcsUtterance?.TrySetResult(true);
			}
		}

		static float NormalizeRate(float rate) =>
			NormalizeRate(rate,
				AVSpeechUtterance.MinimumSpeechRate,
				AVSpeechUtterance.MaximumSpeechRate,
				AVSpeechUtterance.DefaultSpeechRate);
#pragma warning restore CA1416
	}
}
