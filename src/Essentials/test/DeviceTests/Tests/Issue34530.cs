#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

			IEnumerable<Locale> capturedLocales = null;
			var localeQuery = TextToSpeech.Default.GetLocalesAsync();
			var returnedLocales = await localeQuery;
			var queryCompleted = localeQuery.IsCompletedSuccessfully;
			capturedLocales = returnedLocales;

			Assert.True(queryCompleted, "The text-to-speech locale query did not complete successfully.");
			Assert.NotNull(capturedLocales);

			var languages = capturedLocales
				.Select(locale => locale.Language)
				.ToArray();
			var hasLithuanian = languages.Any(language =>
				string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
				(language != null && language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase)));

			Assert.True(
				hasLithuanian,
				$"Lithuanian text-to-speech locale was absent; observed languages: {string.Join(", ", languages)}");
		}
	}
}
#endif

