using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	public Issue35624()
	{
		Items.Add(new ShellContent
		{
			Title = "Character spacing",
			ContentTemplate = new DataTemplate(() => new Issue35624Page(this))
		});
	}

	class Issue35624Page : ContentPage
	{
		const double CharacterSpacing = 8;

		readonly Shell _shell;
		readonly SearchHandler _searchHandler;
		readonly Label _defaultReference;
		readonly Label _spacedReference;
		readonly Label _configuredSpacingLabel;
		readonly Label _initialStateLabel;
		readonly Label _defaultKerningLabel;
		readonly Label _referenceKerningLabel;
		readonly Label _queryLabel;
		readonly Label _callbackLabel;
		readonly Label _searchKerningLabel;

		public Issue35624Page(Shell shell)
		{
			_shell = shell;
			_searchHandler = new SearchHandler
			{
				AutomationId = "Issue35624SearchHandler",
				CharacterSpacing = CharacterSpacing,
				Placeholder = "Search"
			};
			Shell.SetSearchHandler(this, _searchHandler);

			_defaultReference = new Label
			{
				FontSize = 17,
				Text = "Default spacing: SPACING"
			};
			_spacedReference = new Label
			{
				AutomationId = "Issue35624ExpectedSpacingReference",
				CharacterSpacing = CharacterSpacing,
				FontSize = 17,
				Text = "Expected spacing: SPACING"
			};
			_configuredSpacingLabel = CreateMeasurementLabel(
				"Issue35624ConfiguredSpacing",
				$"ConfiguredSpacing: {_searchHandler.CharacterSpacing.ToString(CultureInfo.InvariantCulture)}");
			_initialStateLabel = CreateMeasurementLabel("Issue35624InitialState", "Query=<empty>; Callback=-1");
			_defaultKerningLabel = CreateMeasurementLabel("Issue35624DefaultKerning", "DefaultKerning: -1");
			_referenceKerningLabel = CreateMeasurementLabel("Issue35624ReferenceKerning", "ReferenceKerning: -1");
			_queryLabel = CreateMeasurementLabel("Issue35624Query", "Query: <empty>");
			_callbackLabel = CreateMeasurementLabel("Issue35624Callback", "Callback: -1");
			_searchKerningLabel = CreateMeasurementLabel("Issue35624SearchKerning", "SearchKerning: -1");

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "Character spacing"
					},
					new Label
					{
						Text = "Tap the search field and type SPACING. The search text should match the wide-spaced reference below."
					},
					_defaultReference,
					_spacedReference,
					_configuredSpacingLabel,
					_initialStateLabel,
					_defaultKerningLabel,
					_referenceKerningLabel,
					_queryLabel,
					_callbackLabel,
					_searchKerningLabel
				}
			};

#if IOS
			Loaded += OnPageLoaded;
			_searchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;
#endif
		}

		static Label CreateMeasurementLabel(string automationId, string text) =>
			new()
			{
				AutomationId = automationId,
				Text = text
			};

#if IOS
		void OnPageLoaded(object sender, EventArgs e)
		{
			Dispatcher.Dispatch(() =>
			{
				_defaultKerningLabel.Text = $"DefaultKerning: {GetLabelKerning(_defaultReference).ToString(CultureInfo.InvariantCulture)}";
				_referenceKerningLabel.Text = $"ReferenceKerning: {GetLabelKerning(_spacedReference).ToString(CultureInfo.InvariantCulture)}";
			});
		}

		void OnSearchHandlerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName != SearchHandler.QueryProperty.PropertyName || _searchHandler.Query != "SPACING")
				return;

			_queryLabel.Text = "Query: SPACING";
			Dispatcher.Dispatch(() =>
			{
				var textField = FindSearchTextField();
				_initialStateLabel.Text = string.Format(
					CultureInfo.InvariantCulture,
					"SearchField={0}; Text={1}; Callback=1",
					textField is not null,
					string.IsNullOrEmpty(textField?.Text) ? "<empty>" : textField.Text);
				_searchKerningLabel.Text = $"SearchKerning: {GetTextKerning(textField).ToString(CultureInfo.InvariantCulture)}";
				_callbackLabel.Text = "Callback: 1";
			});
		}

		static double GetLabelKerning(Label label)
		{
			if (label.Handler?.PlatformView is not UIKit.UILabel nativeLabel)
				return -1;

			return GetAttributedKerning(nativeLabel.AttributedText);
		}

		static double GetTextKerning(UIKit.UITextField textField)
		{
			if (textField is null)
				return -1;

			return GetAttributedKerning(textField.AttributedText);
		}

		static double GetAttributedKerning(Foundation.NSAttributedString attributedText)
		{
			if (attributedText is null || attributedText.Length == 0)
				return 0;

			var value = attributedText.GetAttribute(UIKit.UIStringAttributeKey.KerningAdjustment, 0, out _);
			return value is Foundation.NSNumber number ? number.DoubleValue : 0;
		}

		UIKit.UITextField FindSearchTextField()
		{
			if (_shell.Handler is not Microsoft.Maui.IPlatformViewHandler shellHandler)
				return null;

			var searchBar = FindSearchBar(shellHandler.ViewController);
			return FindDescendant<UIKit.UITextField>(searchBar);
		}

		static UIKit.UISearchBar FindSearchBar(UIKit.UIViewController viewController)
		{
			if (viewController is null)
				return null;
			if (viewController.NavigationItem.SearchController?.SearchBar is UIKit.UISearchBar searchBar)
				return searchBar;

			foreach (var child in viewController.ChildViewControllers)
			{
				var descendant = FindSearchBar(child);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}

		static T FindDescendant<T>(UIKit.UIView view) where T : UIKit.UIView
		{
			if (view is T match)
				return match;
			if (view is null)
				return null;

			foreach (var child in view.Subviews)
			{
				var descendant = FindDescendant<T>(child);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}
#endif
	}
}

