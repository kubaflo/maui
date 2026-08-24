#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	[Category("TextToSpeech")]
	[Category("Issue34530")]
	public class Issue34530
	{
		[Fact]
		public async Task GetLocalesIncludesLithuanian()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			IEnumerable<Locale> localesResult = null;
			localesResult = await TextToSpeech.Default.GetLocalesAsync();

			Assert.NotNull(localesResult);

			var locales = localesResult.ToList();
			Assert.NotEmpty(locales);

			var languages = locales.Select(locale => locale.Language).ToList();
			var hasLithuanian = languages.Any(language =>
				string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
				language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));

			Assert.True(
				hasLithuanian,
				$"TextToSpeech.Default.GetLocalesAsync() returned no Lithuanian locale. Total locales: {locales.Count}. Languages: {string.Join(", ", languages)}");
		}
	}
}
#endif

