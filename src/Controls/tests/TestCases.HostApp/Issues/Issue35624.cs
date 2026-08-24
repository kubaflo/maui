#if IOS
using System.Globalization;
using Foundation;
using Microsoft.Maui;
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35624, "SearchHandler CharacterSpacing is not applied", PlatformAffected.iOS)]
public class Issue35624 : Shell
{
	public Issue35624()
	{
		Items.Add(new ShellContent
		{
			Title = "SearchHandler spacing",
			ContentTemplate = new DataTemplate(() => new Issue35624Page())
		});
	}

	sealed class Issue35624Page : ContentPage
	{
		const string TestQuery = "SPACING";
		const double ExpectedCharacterSpacing = 10;

		readonly SearchHandler _searchHandler;
		readonly Label _referenceLabel;
		readonly Label _initialStateLabel;
		readonly Label _inspectionLabel;
		readonly Label _kerningLabel;

		public Issue35624Page()
		{
			Title = "SearchHandler spacing";

			_searchHandler = new SearchHandler
			{
				AutomationId = "Issue35624SearchHandler",
				CharacterSpacing = ExpectedCharacterSpacing,
				Placeholder = "Type SPACING",
				SearchBoxVisibility = SearchBoxVisibility.Collapsible
			};
			_searchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;
			Shell.SetSearchHandler(this, _searchHandler);

			_referenceLabel = new Label
			{
				AutomationId = "Issue35624Reference",
				CharacterSpacing = ExpectedCharacterSpacing,
				FontSize = 18,
				Text = TestQuery
			};
			_initialStateLabel = new Label
			{
				AutomationId = "Issue35624InitialState",
				Text = "InitialReady=False"
			};
			_inspectionLabel = new Label
			{
				AutomationId = "Issue35624Inspection",
				Text = "PENDING"
			};
			_kerningLabel = new Label
			{
				AutomationId = "Issue35624Kerning",
				Text = "PENDING"
			};

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "SearchHandler CharacterSpacing"
					},
					new Label
					{
						Text = "Tap the search icon and type SPACING. The search text should have the same wide spacing as the reference below."
					},
					new Label { Text = "Expected spacing reference:" },
					_referenceLabel,
					_initialStateLabel,
					_inspectionLabel,
					_kerningLabel
				}
			};
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			Dispatcher.Dispatch(UpdateInitialState);
		}

		void OnSearchHandlerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == SearchHandler.QueryProperty.PropertyName && _searchHandler.Query == TestQuery)
				InspectNativeSearchText();
		}

		void UpdateInitialState()
		{
#if IOS
			if (_referenceLabel.Handler?.PlatformView is not UILabel nativeLabel ||
				nativeLabel.AttributedText is not NSAttributedString attributedText ||
				attributedText.Length != TestQuery.Length ||
				attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out var range) is not NSNumber kerning)
			{
				_initialStateLabel.Text = "InitialReady=False";
				return;
			}

			_initialStateLabel.Text =
				$"InitialReady=True;QueryEmpty={string.IsNullOrEmpty(_searchHandler.Query)};" +
				$"CharacterSpacing={Format(ExpectedCharacterSpacing)};ReferenceText={attributedText.Value};" +
				$"ReferenceRange={range.Location},{range.Length};ReferenceKerning={Format(kerning.DoubleValue)}";
#endif
		}

		void InspectNativeSearchText()
		{
#if IOS
			if (Handler is not IPlatformViewHandler pageHandler)
			{
				_inspectionLabel.Text = "Callback=True;NativeField=False";
				return;
			}

			var pageViewController = pageHandler.ViewController;
			if (pageViewController is null)
			{
				_inspectionLabel.Text = "Callback=True;NativeField=False";
				return;
			}

			var viewController = pageViewController.NavigationController?.TopViewController ?? pageViewController;
			var searchField = viewController.NavigationItem.SearchController?.SearchBar.SearchTextField;
			var attributedText = searchField?.AttributedText;
			if (searchField is null || attributedText is null)
			{
				_inspectionLabel.Text = "Callback=True;NativeField=False";
				return;
			}

			var attribute = attributedText.GetAttribute(UIStringAttributeKey.KerningAdjustment, 0, out _);
			var attached = searchField.Window is not null;
			var visible = !searchField.Hidden && searchField.Alpha > 0;

			_inspectionLabel.Text =
				$"Callback=True;NativeField=True;Text={attributedText.Value};Range=0,{attributedText.Length};" +
				$"Attached={attached};Visible={visible}";
			_kerningLabel.Text = attribute is NSNumber kerning ? Format(kerning.DoubleValue) : "0";
#endif
		}

#if IOS
		static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
#endif
	}
}

