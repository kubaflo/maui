#if IOS
using System.Globalization;
using Foundation;
using UIKit;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing property is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	const string SearchText = "SPACING";

	readonly ContentPage _contentPage;
	readonly Label _queryStatus;
	readonly Label _reference;
	readonly Label _resultStatus;
	double _referenceKerning = -1;
	double _searchKerning = -1;
	int _measurementGeneration = -1;

	public Issue35624()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		_reference = new Label
		{
			AutomationId = "Issue35624Reference",
			Text = SearchText,
			CharacterSpacing = 10,
			FontSize = 24
		};

		_queryStatus = new Label
		{
			AutomationId = "Issue35624QueryStatus",
			Text = "query=NOT_RECEIVED"
		};

		_resultStatus = new Label
		{
			AutomationId = "Issue35624ResultStatus",
			Text = "generation=-1;reference=-1;search=-1"
		};

		_contentPage = new ContentPage
		{
			Title = "SearchHandler CharacterSpacing",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						AutomationId = "Issue35624PageStatus",
						Text = "Type SPACING in the Shell search field. The reference below uses CharacterSpacing 10.",
						FontSize = 18
					},
					_reference,
					_queryStatus,
					_resultStatus
				}
			}
		};

		var searchHandler = new Issue35624SearchHandler(OnQueryChanged)
		{
			CharacterSpacing = 10,
			Placeholder = "Type SPACING",
			SearchBoxVisibility = SearchBoxVisibility.Expanded
		};
		Shell.SetSearchHandler(_contentPage, searchHandler);

		Items.Add(new ShellContent { Content = _contentPage });
		_reference.Loaded += OnReferenceLoaded;
	}

	void OnReferenceLoaded(object sender, EventArgs e)
	{
		_reference.Loaded -= OnReferenceLoaded;

		if (_reference.Handler.PlatformView is UILabel platformLabel && platformLabel.AttributedText is NSAttributedString attributedText)
		{
			_referenceKerning = ReadKerning(attributedText);
			UpdateResultStatus();
		}
	}

	void OnQueryChanged(string query)
	{
		_queryStatus.Text = $"query={query}";

		if (query == SearchText)
			_contentPage.Dispatcher.Dispatch(MeasureSearchKerning);
	}

	void MeasureSearchKerning()
	{
		if (_contentPage.Handler.PlatformView is not UIView pageView ||
			pageView.Window is not UIWindow window ||
			FindDescendant<UISearchBar>(window) is not UISearchBar searchBar ||
			searchBar.SearchTextField.AttributedText is not NSAttributedString attributedText)
		{
			return;
		}

		_searchKerning = ReadKerning(attributedText);
		_measurementGeneration = 1;
		UpdateResultStatus();
	}

	void UpdateResultStatus()
	{
		_resultStatus.Text =
			$"generation={_measurementGeneration};reference={_referenceKerning.ToString("0.####", CultureInfo.InvariantCulture)};search={_searchKerning.ToString("0.####", CultureInfo.InvariantCulture)}";
	}

	static double ReadKerning(NSAttributedString attributedText)
	{
		if (attributedText.Length == 0)
			return 0;

		var value = attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out _);
		return value is NSNumber number ? number.DoubleValue : 0;
	}

	static T FindDescendant<T>(UIView view) where T : UIView
	{
		if (view is T match)
			return match;

		foreach (var child in view.Subviews)
		{
			var descendant = FindDescendant<T>(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}

	sealed class Issue35624SearchHandler : SearchHandler
	{
		readonly Action<string> _queryChanged;

		public Issue35624SearchHandler(Action<string> queryChanged)
		{
			_queryChanged = queryChanged;
		}

		protected override void OnQueryChanged(string oldValue, string newValue)
		{
			base.OnQueryChanged(oldValue, newValue);
			_queryChanged(newValue);
		}
	}
}
#endif

