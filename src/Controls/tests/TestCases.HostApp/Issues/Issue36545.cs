using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36545, "Grouped CollectionView with GridItemsLayout omits spacing after group header", PlatformAffected.iOS)]
public class Issue36545 : ContentPage
{
	const double ExpectedSpacing = 30;

	public Issue36545()
	{
		var showIssueButton = new Button
		{
			Text = "Show grouped CollectionView",
			AutomationId = "ShowIssueButton",
		};
		showIssueButton.Clicked += OnShowIssueClicked;

		Content = new Grid
		{
			AutomationId = "InitialRoot",
			Padding = 24,
			Children =
			{
				new VerticalStackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					Spacing = 18,
					Children =
					{
						new Label
						{
							Text = "Issue 36545: grouped grid spacing",
							FontSize = 22,
							HorizontalTextAlignment = TextAlignment.Center,
						},
						showIssueButton,
					},
				},
			},
		};
	}

	void OnShowIssueClicked(object sender, EventArgs e)
	{
		var collectionView = CreateCollectionView();

#if IOS
		var handler = new Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2();
		handler.SetMauiContext(Handler!.MauiContext!);
		collectionView.Handler = handler;
#endif

		Content = collectionView;
	}

	static CollectionView CreateCollectionView()
	{
		var collectionView = new CollectionView
		{
			AutomationId = "TestCollectionView",
			IsGrouped = true,
			HorizontalOptions = LayoutOptions.Fill,
			Margin = new Thickness(5, 30, 5, 5),
			ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical)
			{
				Span = 5,
				VerticalItemSpacing = ExpectedSpacing,
				HorizontalItemSpacing = 10,
			},
			GroupHeaderTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					HorizontalOptions = LayoutOptions.Fill,
					HorizontalTextAlignment = TextAlignment.Start,
					Padding = 10,
					FontSize = 18,
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					BackgroundColor = Colors.Gray,
				};
				label.SetBinding(Label.TextProperty, nameof(NumberGroup.Name));
				label.SetBinding(AutomationIdProperty, nameof(NumberGroup.Name), stringFormat: "Group{0}");
				return label;
			}),
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					TextColor = Colors.White,
					VerticalOptions = LayoutOptions.Center,
					HorizontalTextAlignment = TextAlignment.Center,
				};
				label.SetBinding(Label.TextProperty, ".");

				var border = new Border
				{
					StrokeShape = new RoundRectangle { CornerRadius = 10 },
					Padding = 5,
					MinimumWidthRequest = 50,
					Stroke = Colors.Transparent,
					BackgroundColor = Colors.Gray,
					StrokeThickness = 1,
					HorizontalOptions = LayoutOptions.Center,
					Content = label,
				};
				border.SetBinding(AutomationIdProperty, ".", stringFormat: "Item{0}");
				return border;
			}),
		};

		collectionView.ItemsSource = new ObservableCollection<NumberGroup>
		{
			new("100s", new List<string>
			{
				"100", "200", "300", "400", "500",
				"600", "700", "800", "900",
			}),
			new("1000s", new List<string>
			{
				"1000", "2000", "3000", "4000", "5000",
				"6000", "7000", "8000", "9000",
			}),
		};

		return collectionView;
	}

	sealed class NumberGroup : ObservableCollection<string>
	{
		public NumberGroup(string name, IEnumerable<string> numbers)
			: base(numbers)
		{
			Name = name;
		}

		public string Name { get; }
	}
}

