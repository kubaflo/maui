#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	[Category("TextToSpeech", "Issue34530")]
	public class Issue34530
	{
#if !MACCATALYST
		[Fact]
		public async Task GetLocalesIncludesLithuanian()
		{
			IEnumerable<Locale> observedLocales = null;
			var queryCompleted = false;
			ITextToSpeech textToSpeech = TextToSpeech.Default;

			observedLocales = await textToSpeech.GetLocalesAsync();
			queryCompleted = true;

			Assert.True(queryCompleted);
			Assert.NotNull(observedLocales);

			var locales = observedLocales.ToList();
			Assert.All(locales, locale => Assert.False(string.IsNullOrEmpty(locale.Language)));

			Assert.True(
				locales.Any(locale =>
					string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
					locale.Language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase)),
				$"TextToSpeech.Default.GetLocalesAsync returned {locales.Count} locales, but no Lithuanian language (lt or lt-*).");
		}
#endif
	}
}
#endif
