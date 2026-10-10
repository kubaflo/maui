#if IOS && !MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	[Category("Issue34530")]
	public class Issue34530
	{
		[Fact]
		public async Task GetLocalesAsyncIncludesLithuanian()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			Locale[] locales = null;
			bool queryCompleted = false;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				locales = (await TextToSpeech.Default.GetLocalesAsync()).ToArray();
				queryCompleted = true;
			});

			Assert.True(queryCompleted, "TextToSpeech.GetLocalesAsync did not complete.");
			Assert.NotNull(locales);
			Assert.NotEmpty(locales);
			Assert.All(
				locales,
				locale => Assert.False(
					string.IsNullOrWhiteSpace(locale.Language),
					"TextToSpeech.GetLocalesAsync returned a locale without a language code."));

			string[] languageCodes = locales
				.Select(locale => locale.Language)
				.OrderBy(language => language, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			bool containsLithuanian = languageCodes.Any(language =>
				string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
				language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));

			Assert.True(
				containsLithuanian,
				$"TextToSpeech.GetLocalesAsync omitted Lithuanian. Returned language codes: {string.Join(", ", languageCodes)}");
		}
	}
}
#endif

