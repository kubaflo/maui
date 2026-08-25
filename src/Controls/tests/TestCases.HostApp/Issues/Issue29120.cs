#if WINDOWS
using System.Collections.ObjectModel;
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29120, "CollectionView jumps to the top during incremental loading", PlatformAffected.UWP)]
public class Issue29120 : ContentPage
{
	const int PageSize = 10;
	const int MaximumItemCount = 50;

	public ObservableCollection<AnimalItem> Animals { get; } = [];

	public Command LoadMoreDataCommand { get; }

	public Issue29120()
	{
		Title = "Incremental loading on scroll";

		var measurementLabel = new Label
		{
			AutomationId = "MeasurementStatus",
			Text = "Attached=False;Generation=-1;Count=-1;PreOffset=-1;PostOffset=-1;PostCallback=False"
		};

		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "Reached100=False;Reached250=False"
		};

		var collectionView = new CollectionView
		{
			AutomationId = "AnimalsCollectionView",
			ItemsSource = Animals,
			RemainingItemsThreshold = 5,
			ItemTemplate = new DataTemplate(CreateAnimalTemplate)
		};

		double latestOffset = -1;
		double preLoadOffset = -1;
		int loadGeneration = 0;
		bool postLoadObservationPending = false;
		bool isLoading = false;

		LoadMoreDataCommand = new Command(() =>
		{
			if (isLoading || Animals.Count >= MaximumItemCount)
				return;

			isLoading = true;
			preLoadOffset = latestOffset;
			loadGeneration++;
			postLoadObservationPending = preLoadOffset > 100;
			AddAnimals();
			isLoading = false;
		});

		collectionView.RemainingItemsThresholdReachedCommand = LoadMoreDataCommand;
		collectionView.RemainingItemsThresholdReached += (_, _) => resultLabel.Text = "Loading ten more animals.";
		collectionView.Scrolled += (_, args) =>
		{
			latestOffset = args.VerticalOffset;
			resultLabel.Text = latestOffset > 250
				? "Reached100=True;Reached250=True"
				: latestOffset > 100
					? "Reached100=True;Reached250=False"
					: "Reached100=False;Reached250=False";

			if (postLoadObservationPending)
			{
				postLoadObservationPending = false;
				measurementLabel.Text = FormatPostLoadStatus(
					collectionView.Handler is not null,
					loadGeneration,
					Animals.Count,
					preLoadOffset,
					latestOffset);
				resultLabel.Text = latestOffset <= 20
					? "The CollectionView returned to the first items."
					: "The CollectionView retained its scrolled position.";
			}
		};

		AddAnimals();
		BindingContext = this;

		var header = new StackLayout
		{
			Children =
			{
				new Label { Text = "Scroll down. Ten more animals load when five items remain." },
				resultLabel,
				measurementLabel
			}
		};

		var root = new Grid
		{
			Margin = 20,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		root.Add(header);
		root.Add(collectionView, 0, 1);
		Content = root;
	}

	void AddAnimals()
	{
		int firstItem = Animals.Count + 1;
		for (int index = firstItem; index < firstItem + PageSize; index++)
		{
			Animals.Add(new AnimalItem
			{
				Name = $"Animal {index:00}",
				Location = $"Habitat {index:00}"
			});
		}
	}

	static View CreateAnimalTemplate()
	{
		var image = new Image
		{
			Aspect = Aspect.AspectFill,
			HeightRequest = 60,
			WidthRequest = 60,
			Source = "dotnet_bot.png"
		};
		Grid.SetRowSpan(image, 2);

		var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
		nameLabel.SetBinding(Label.TextProperty, nameof(AnimalItem.Name));
		Grid.SetColumn(nameLabel, 1);

		var locationLabel = new Label
		{
			FontAttributes = FontAttributes.Italic,
			VerticalOptions = LayoutOptions.End
		};
		locationLabel.SetBinding(Label.TextProperty, nameof(AnimalItem.Location));
		Grid.SetRow(locationLabel, 1);
		Grid.SetColumn(locationLabel, 1);

		return new Grid
		{
			Padding = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star)
			},
			Children = { image, nameLabel, locationLabel }
		};
	}

	static string FormatPostLoadStatus(bool attached, int generation, int count, double preOffset, double postOffset) =>
		string.Format(
			CultureInfo.InvariantCulture,
			"Attached={0};Generation={1};Count={2};PreOffset={3:F1};PostOffset={4:F1};PostCallback=True",
			attached,
			generation,
			count,
			preOffset,
			postOffset);

	public sealed class AnimalItem
	{
		public required string Name { get; init; }

		public required string Location { get; init; }
	}
}
#endif

