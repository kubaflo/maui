#if IOS
using System.Globalization;
using Foundation;
using UIKit;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	const double RequestedCharacterSpacing = 10;
	readonly Label _inputStatusLabel;
	readonly Label _measurementLabel;
	readonly Label _referenceLabel;
	readonly SearchHandler _searchHandler;

	public Issue35624()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		_searchHandler = new SearchHandler
		{
			CharacterSpacing = RequestedCharacterSpacing,
			Placeholder = "Character spacing"
		};

		_referenceLabel = new Label
		{
			CharacterSpacing = RequestedCharacterSpacing,
			Text = "MAUI TEST"
		};

		_inputStatusLabel = new Label
		{
			AutomationId = "Issue35624InputStatus",
			Text = "Input received: none"
		};

		_measurementLabel = new Label
		{
			AutomationId = "Issue35624Measurement",
			Text = "waiting"
		};

		var contentPage = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					_referenceLabel,
					_inputStatusLabel,
					_measurementLabel
				}
			}
		};

		SetSearchHandler(contentPage, _searchHandler);
		_searchHandler.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName != nameof(SearchHandler.Query) || string.IsNullOrEmpty(_searchHandler.Query))
				return;

			_inputStatusLabel.Text = $"Input received: {_searchHandler.Query}";
			Dispatcher.Dispatch(MeasureCharacterSpacing);
		};

		Items.Add(new ShellContent { Content = contentPage });
	}

	void MeasureCharacterSpacing()
	{
		if (_referenceLabel.Handler?.PlatformView is not UILabel nativeLabel)
		{
			_measurementLabel.Text = "Reference: unavailable";
			return;
		}

		var referenceAttributedText = nativeLabel.AttributedText;
		if (referenceAttributedText is null ||
			!TryGetKerning(referenceAttributedText, out var referenceKerning))
		{
			_measurementLabel.Text = "Reference: unavailable";
			return;
		}

		if (Window?.Handler?.PlatformView is not UIWindow platformWindow ||
			!TryFindSubview(platformWindow, out UISearchBar searchBar))
		{
			_measurementLabel.Text = $"Reference: {Format(referenceKerning)}; SearchHandler: unavailable";
			return;
		}

		var searchAttributedText = searchBar.SearchTextField.AttributedText;
		if (searchAttributedText is null ||
			!TryGetKerning(searchAttributedText, out var searchKerning))
		{
			_measurementLabel.Text = $"Reference: {Format(referenceKerning)}; SearchHandler: unavailable";
			return;
		}

		_measurementLabel.Text =
			$"Reference: {Format(referenceKerning)}; SearchHandler: {Format(searchKerning)}";
	}

	static string Format(double value) =>
		value.ToString("0.##", CultureInfo.InvariantCulture);

	static bool TryGetKerning(NSAttributedString attributedText, out double kerning)
	{
		kerning = double.NaN;
		if (attributedText.Length == 0)
			return false;

		var value = attributedText.GetAttribute(
			UIStringAttributeKey.KerningAdjustment,
			0,
			out _);
		if (value is null)
		{
			kerning = 0;
			return true;
		}

		if (value is not NSNumber number)
			return false;

		kerning = number.DoubleValue;
		return true;
	}

	static bool TryFindSubview<T>(UIView view, out T match) where T : UIView
	{
		if (view is T typedView)
		{
			match = typedView;
			return true;
		}

		foreach (var subview in view.Subviews)
		{
			if (TryFindSubview(subview, out match))
				return true;
		}

		match = null!;
		return false;
	}
}
#endif

