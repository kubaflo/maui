namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	const string QueryText = "MAUI SEARCH";
	const double CharacterSpacing = 12;

	readonly Label _callbackSequenceLabel;
	readonly Label _configuredSpacingLabel;
	readonly Label _inspectionLabel;
	readonly Label _managedQueryLabel;
	readonly Label _nativeStateLabel;
	readonly Label _referenceKerningLabel;
	readonly Label _searchKerningLabel;
	readonly Label _spacingReferenceLabel;
	readonly SearchHandler _issueSearchHandler;
	int _callbackSequence = -1;

	public Issue35624()
	{
		_callbackSequenceLabel = CreateDiagnosticLabel("Issue35624CallbackSequence", "Callback: -1");
		_configuredSpacingLabel = CreateDiagnosticLabel("Issue35624ConfiguredSpacing", "Configured spacing: 12");
		_inspectionLabel = CreateDiagnosticLabel("Issue35624Inspection", "Inspection: pending");
		_managedQueryLabel = CreateDiagnosticLabel("Issue35624ManagedQuery", "Query: <empty>");
		_nativeStateLabel = CreateDiagnosticLabel("Issue35624NativeState", "Native attached: False; text: <empty>");
		_referenceKerningLabel = CreateDiagnosticLabel("Issue35624ReferenceKerning", "Reference kerning: -1; full range: False");
		_searchKerningLabel = CreateDiagnosticLabel("Issue35624SearchKerning", "Search kerning: -1; full range: False");
		_spacingReferenceLabel = new Label
		{
			AutomationId = "Issue35624Reference",
			CharacterSpacing = CharacterSpacing,
			FontSize = 22,
			Text = QueryText
		};

		_issueSearchHandler = new SearchHandler
		{
			AutomationId = "Issue35624SearchHandler",
			CharacterSpacing = CharacterSpacing,
			Placeholder = "Type MAUI SEARCH",
			SearchBoxVisibility = SearchBoxVisibility.Expanded
		};
		_issueSearchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;

		var contentPage = new ContentPage
		{
			Title = "Character Spacing",
			Content = new VerticalStackLayout
			{
				Margin = new Thickness(24),
				Spacing = 18,
				Children =
				{
					_spacingReferenceLabel,
					_configuredSpacingLabel,
					_managedQueryLabel,
					_callbackSequenceLabel,
					_nativeStateLabel,
					_inspectionLabel,
					_referenceKerningLabel,
					_searchKerningLabel
				}
			}
		};

		Shell.SetSearchHandler(contentPage, _issueSearchHandler);
		Items.Add(new ShellContent
		{
			Content = contentPage,
			Title = "Spacing"
		});
	}

	static Label CreateDiagnosticLabel(string automationId, string text)
	{
		return new Label
		{
			AutomationId = automationId,
			Text = text
		};
	}

	void OnSearchHandlerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != SearchHandler.QueryProperty.PropertyName)
			return;

		var query = _issueSearchHandler.Query;
		_managedQueryLabel.Text = $"Query: {(string.IsNullOrEmpty(query) ? "<empty>" : query)}";

		if (query != QueryText)
			return;

		_callbackSequence++;
		_callbackSequenceLabel.Text = $"Callback: {_callbackSequence}";
		Dispatcher.Dispatch(InspectNativeState);
	}

	void InspectNativeState()
	{
#if IOS
		var referenceLabel = _spacingReferenceLabel.Handler?.PlatformView as UIKit.UILabel;
		UIKit.UIView rootView = referenceLabel;
		while (rootView?.Superview is UIKit.UIView superview)
			rootView = superview;

		var searchBar = rootView is null
			? null
			: FindDescendant<UIKit.UISearchBar>(rootView, candidate => candidate.SearchTextField.Text == QueryText);
		var searchField = searchBar?.SearchTextField;

		var nativeAttached = searchBar?.Window is not null && searchField?.Window is not null;
		_nativeStateLabel.Text = $"Native attached: {nativeAttached}; text: {searchField?.Text ?? "<empty>"}";

		var referenceKerning = ReadKerning(referenceLabel?.AttributedText);
		_referenceKerningLabel.Text = $"Reference kerning: {Format(referenceKerning.Value)}; full range: {referenceKerning.FullRange}";

		var searchKerning = ReadKerning(searchField?.AttributedText);
		_searchKerningLabel.Text = $"Search kerning: {Format(searchKerning.Value)}; full range: {searchKerning.FullRange}";
		_inspectionLabel.Text = "Inspection: complete";
#endif
	}

#if IOS
	static T FindDescendant<T>(UIKit.UIView view, Func<T, bool> predicate) where T : UIKit.UIView
	{
		if (view is T candidate && predicate(candidate))
			return candidate;

		foreach (var subview in view.Subviews)
		{
			var match = FindDescendant<T>(subview, predicate);
			if (match is not null)
				return match;
		}

		return null;
	}

	static (double Value, bool FullRange) ReadKerning(Foundation.NSAttributedString attributedText)
	{
		if (attributedText is null || attributedText.Length == 0)
			return (0, false);

		var attribute = attributedText.GetAttribute(
			UIKit.UIStringAttributeKey.KerningAdjustment,
			0,
			out var effectiveRange);
		var value = attribute is Foundation.NSNumber number ? number.DoubleValue : 0;
		return (value, effectiveRange.Location == 0 && effectiveRange.Length == attributedText.Length);
	}

	static string Format(double value) =>
		value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
#endif
}

