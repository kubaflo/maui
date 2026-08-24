#if IOS && !MACCATALYST
using System.Globalization;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Maui.Controls.Sample.Issues;

public partial class Issue35624
{
	partial void MeasureNativeKerning(
		SearchHandler searchHandler,
		Label referenceLabel,
		Label statusLabel,
		int sequence)
	{
		var query = searchHandler.Query ?? string.Empty;

		if (Handler?.PlatformView is not UIView shellView ||
			shellView.FindDescendantView<UISearchTextField>() is not UISearchTextField searchField ||
			referenceLabel.Handler?.PlatformView is not UILabel nativeReferenceLabel)
		{
			statusLabel.Text = $"Sequence={sequence};Measurement=Unavailable";
			return;
		}

		var referenceAttributedText = nativeReferenceLabel.AttributedText;
		var searchAttributedText = searchField.AttributedText;
		var referenceKerning = referenceAttributedText is null ? 0 : ReadKerning(referenceAttributedText);
		var searchKerning = searchAttributedText is null ? 0 : ReadKerning(searchAttributedText);

		statusLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"Sequence={sequence};Query={query};NativeText={searchField.Text ?? string.Empty};ReferenceKerning={referenceKerning:0.###};SearchKerning={searchKerning:0.###}");
	}

	static double ReadKerning(NSAttributedString attributedText)
	{
		if (attributedText.Length == 0)
			return 0;

		var value = attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out _);
		return value is NSNumber number ? number.DoubleValue : 0;
	}
}
#endif
