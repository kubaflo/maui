using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.WinRT)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var wrappedCollection = new CollectionView
		{
			ItemsSource = new[] { "Item" },
			ItemTemplate = new DataTemplate(() => new Grid
			{
				Children =
				{
					new BoundsContentView("Wrapped")
				}
			})
		};

		var directCollection = new CollectionView
		{
			ItemsSource = new[] { "Item" },
			ItemTemplate = new DataTemplate(() => new BoundsContentView("Direct"))
		};

		var layout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
			},
			RowSpacing = 12,
			ColumnSpacing = 12
		};

		var instructionLabel = new Label
		{
			Text = "Tap each custom ContentView after it is rendered.",
			FontSize = 18
		};
		layout.Add(instructionLabel, 0, 0);
		Grid.SetColumnSpan(instructionLabel, 2);

		layout.Add(new Label { Text = "Grid-wrapped template" }, 0, 1);
		layout.Add(new Label { Text = "Direct template" }, 1, 1);
		layout.Add(wrappedCollection, 0, 2);
		layout.Add(directCollection, 1, 2);

		Content = layout;
	}

	sealed class BoundsContentView : ContentView
	{
		public BoundsContentView(string idPrefix)
		{
			AutomationId = $"{idPrefix}Item";
			var tapTarget = new Label
			{
				AutomationId = $"{idPrefix}TapTarget",
				Text = "Tap this custom ContentView",
				Padding = new Thickness(16),
				FontSize = 18
			};

			var measurementStatus = new Label
			{
				AutomationId = $"{idPrefix}MeasurementStatus",
				Text = "Gesture Width/Height: not measured"
			};

			Content = new VerticalStackLayout
			{
				Children =
					{
						tapTarget,
						measurementStatus
					}
			};

			var tapGesture = new TapGestureRecognizer();
			tapGesture.Tapped += (_, _) =>
			{
				measurementStatus.Text = string.Create(
					CultureInfo.InvariantCulture,
					$"Gesture Width={Width:R}, Height={Height:R}");
			};
			GestureRecognizers.Add(tapGesture);
		}
	}
}

