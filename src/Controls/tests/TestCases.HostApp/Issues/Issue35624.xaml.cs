using System.Globalization;

#if IOS
using Foundation;
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing property is not applied", PlatformAffected.iOS)]
public partial class Issue35624 : Shell
{
	int _queryChangedCount;

	public Issue35624()
	{
		InitializeComponent();
		ConfigurationStatus.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"CharacterSpacing={SearchInput.CharacterSpacing}; Placeholder={SearchInput.Placeholder}; Visibility={SearchInput.SearchBoxVisibility}; Query={(SearchInput.Query ?? "<empty>")}");
		SearchInput.PropertyChanged += OnSearchInputPropertyChanged;
	}

	void OnSearchInputPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != SearchHandler.QueryProperty.PropertyName)
			return;

		_queryChangedCount++;
		var query = SearchInput.Query ?? string.Empty;
		CallbackStatus.Text = $"Callbacks={_queryChangedCount}; Query={query}";

#if IOS
		if (query == "SPACING")
			MeasureNativeKerning();
#endif
	}

#if IOS
	void MeasureNativeKerning()
	{
		if (ReferenceLabel.Handler?.PlatformView is not UILabel nativeLabel ||
			nativeLabel.Window is not UIWindow window)
		{
			NativeViewStatus.Text = "Search=False; Label=False; AttributedSearch=False; AttributedLabel=False";
			return;
		}

		var searchBar = Descendants(window).OfType<UISearchBar>().FirstOrDefault();
		var searchTextField = searchBar is null
			? null
			: Descendants(searchBar).OfType<UISearchTextField>().FirstOrDefault();
		var searchAttributedText = searchTextField?.AttributedText;
		var labelAttributedText = nativeLabel.AttributedText;

		NativeViewStatus.Text =
			$"Search={searchTextField is not null}; Label=True; AttributedSearch={searchAttributedText is not null}; AttributedLabel={labelAttributedText is not null}";

		if (searchAttributedText is null || labelAttributedText is null)
			return;

		var searchAttribute = searchAttributedText.GetAttribute(
			UIStringAttributeKey.KerningAdjustment, 0, out var searchRange);
		var labelAttribute = labelAttributedText.GetAttribute(
			UIStringAttributeKey.KerningAdjustment, 0, out var labelRange);
		var searchKerning = searchAttribute is NSNumber searchNumber ? searchNumber.DoubleValue : 0;
		var labelKerning = labelAttribute is NSNumber labelNumber ? labelNumber.DoubleValue : 0;

		RangeStatus.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"SearchRange={searchRange.Length}; LabelRange={labelRange.Length}");
		KerningStatus.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"Search={searchKerning}; Label={labelKerning}");
	}

	static IEnumerable<UIView> Descendants(UIView root)
	{
		foreach (var child in root.Subviews)
		{
			yield return child;

			foreach (var descendant in Descendants(child))
				yield return descendant;
		}
	}
#endif
}
