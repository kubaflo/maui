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

			IEnumerable<Locale> localeSnapshot = null;
			var queryCompleted = false;

			localeSnapshot = await TextToSpeech.Default.GetLocalesAsync();
			queryCompleted = true;

			Assert.True(queryCompleted, "The text-to-speech locale query did not complete.");
			Assert.True(localeSnapshot is not null, "The text-to-speech locale query returned null.");

			var locales = localeSnapshot.ToArray();
			Assert.True(locales.Length > 0, "The text-to-speech locale query returned no locales.");

			var hasLithuanian = locales.Any(locale =>
				locale.Language is not null &&
				(string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
					locale.Language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase)));
			var languageCodes = string.Join(", ", locales.Select(locale => locale.Language ?? "<null>"));

			Assert.True(
				hasLithuanian,
				$"Lithuanian text-to-speech locale was absent after GetLocalesAsync completed. Returned {locales.Length} locales: {languageCodes}");
		}
	}
}
#endif

