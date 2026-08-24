#if IOS
using System.Globalization;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing property is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	const string SearchText = "SPACING";
	const double ExpectedCharacterSpacing = 20;

	public Issue35624()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		Items.Add(new ShellContent
		{
			Title = "Issue 35624",
			Route = "Issue35624",
			Content = new Issue35624Page(),
		});
	}

	class Issue35624Page : ContentPage
	{
		readonly Label _measurementLabel;
		readonly Label _queryLabel;
		readonly Label _referenceLabel;
		readonly SearchHandler _searchHandler;

		public Issue35624Page()
		{
			Title = "Issue 35624";

			_searchHandler = new SearchHandler
			{
				AutomationId = "Issue35624SearchHandler",
				CharacterSpacing = ExpectedCharacterSpacing,
				FontSize = 20,
				Placeholder = "SearchHandler",
				SearchBoxVisibility = SearchBoxVisibility.Collapsible,
			};
			Shell.SetSearchHandler(this, _searchHandler);

			_referenceLabel = new Label
			{
				AutomationId = "Issue35624Reference",
				CharacterSpacing = ExpectedCharacterSpacing,
				FontSize = 20,
				Text = SearchText,
			};
			_queryLabel = CreateTelemetryLabel("Issue35624Query", "Query: waiting");
			_measurementLabel = CreateTelemetryLabel("Issue35624Measurement", "Measurement: pending");

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "SearchHandler character spacing",
					},
					new Label
					{
						Text = "Tap the SearchHandler icon and type SPACING. The search text should match the spaced reference below.",
					},
					_referenceLabel,
					_queryLabel,
					_measurementLabel,
				},
			};

			_searchHandler.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == SearchHandler.QueryProperty.PropertyName &&
					_searchHandler.Query == SearchText)
				{
					_queryLabel.Text = $"Query: {SearchText}";
					Dispatcher.Dispatch(MeasureNativeKerning);
				}
			};
		}

		static Label CreateTelemetryLabel(string automationId, string text) =>
			new()
			{
				AutomationId = automationId,
				FontSize = 12,
				Text = text,
			};

		void MeasureNativeKerning()
		{
#if IOS
			var platformWindow = Window?.Handler?.PlatformView as UIKit.UIWindow;
			if (platformWindow is null ||
				!TryFindTextField(platformWindow, SearchText, out var textField) ||
				textField.Handle == IntPtr.Zero)
			{
				_measurementLabel.Text = "Measurement: native SearchHandler unavailable";
				return;
			}

			var searchAttributedText = textField.AttributedText;
			var referenceNativeLabel = _referenceLabel.Handler?.PlatformView as UIKit.UILabel;
			var referenceAttributedText = referenceNativeLabel?.AttributedText;
			if (searchAttributedText is null || referenceAttributedText is null)
			{
				_measurementLabel.Text = "Measurement: attributed text unavailable";
				return;
			}

			_measurementLabel.Text =
				$"Measurement: complete|" +
				$"Search native id: {textField.Handle}|" +
				$"Search attributed text: {searchAttributedText.Value}|" +
				$"Search kerning: {ReadKerning(searchAttributedText).ToString(CultureInfo.InvariantCulture)}|" +
				$"Reference attributed text: {referenceAttributedText.Value}|" +
				$"Reference kerning: {ReadKerning(referenceAttributedText).ToString(CultureInfo.InvariantCulture)}";
#endif
		}

#if IOS
		static bool TryFindTextField(UIKit.UIView view, string text, out UIKit.UITextField textField)
		{
			if (view is UIKit.UITextField candidate && candidate.Text == text)
			{
				textField = candidate;
				return true;
			}

			foreach (var child in view.Subviews)
			{
				if (TryFindTextField(child, text, out textField))
					return true;
			}

			textField = null!;
			return false;
		}

		static double ReadKerning(Foundation.NSAttributedString attributedText)
		{
			if (attributedText.Length == 0)
				return -1;

			var value = attributedText.GetAttribute(UIKit.UIStringAttributeKey.KerningAdjustment, 0, out _);
			return value is Foundation.NSNumber number ? number.DoubleValue : -1;
		}
#endif
	}
}

