#if WINDOWS
using System.Collections.ObjectModel;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 25020, "Duplicated items in searched results", PlatformAffected.UWP)]
public class Issue25020 : ContentPage
{
	readonly Library[] allLibraries =
	[
		new("AAA", "dotnet_bot.png"),
		new("BBB", "dotnet_bot.png"),
		new("CCC", "dotnet_bot.png")
	];
	readonly ObservableCollection<Library> libraries = [];
	readonly HashSet<Label> loadedItemLabels = [];
	readonly Label filterGenerationLabel;
	readonly Label inspectionGenerationLabel;
	readonly Label aaaCountLabel;
	readonly Label bbbCountLabel;
	readonly Label cccCountLabel;
	int filterGeneration = -1;
	int inspectionGeneration = -1;

	public Issue25020()
	{
		filterGenerationLabel = CreateResultLabel("FilterGeneration", filterGeneration);
		inspectionGenerationLabel = CreateResultLabel("InspectionGeneration", inspectionGeneration);
		aaaCountLabel = CreateResultLabel("AaaCount", -1);
		bbbCountLabel = CreateResultLabel("BbbCount", -1);
		cccCountLabel = CreateResultLabel("CccCount", -1);

		var searchEntry = new Entry
		{
			AutomationId = "SearchEntry",
			WidthRequest = 200
		};
		searchEntry.TextChanged += OnSearchTextChanged;

		var inspectButton = new Button
		{
			AutomationId = "InspectButton",
			Text = "Inspect rendered items"
		};
		inspectButton.Clicked += OnInspectClicked;

		var librariesView = new CollectionView
		{
			ItemsSource = libraries,
			Header = new HorizontalStackLayout
			{
				Children =
				{
					searchEntry,
					inspectButton,
					filterGenerationLabel,
					inspectionGenerationLabel,
					aaaCountLabel,
					bbbCountLabel,
					cccCountLabel
				}
			},
			ItemTemplate = new DataTemplate(CreateItemTemplate)
		};

		ReloadLibraries(allLibraries);
		Content = librariesView;
	}

	static Label CreateResultLabel(string automationId, int value) =>
		new()
		{
			AutomationId = automationId,
			Text = value.ToString()
		};

	View CreateItemTemplate()
	{
		var image = new Image
		{
			WidthRequest = 100,
			HeightRequest = 100
		};
		image.SetBinding(Image.SourceProperty, nameof(Library.ImageSource));

		var itemLabel = new Label
		{
			VerticalTextAlignment = TextAlignment.Center
		};
		itemLabel.SetBinding(Label.TextProperty, nameof(Library.Title));
		itemLabel.Loaded += OnItemLabelLoaded;
		itemLabel.Unloaded += OnItemLabelUnloaded;

		return new HorizontalStackLayout
		{
			Children = { image, itemLabel }
		};
	}

	void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		var searchText = e.NewTextValue;
		Dispatcher.Dispatch(() =>
		{
			var matches = string.IsNullOrWhiteSpace(searchText)
				? allLibraries
				: allLibraries.Where(library =>
					library.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));

			ReloadLibraries(matches);
			filterGeneration++;
			filterGenerationLabel.Text = filterGeneration.ToString();
		});
	}

	void ReloadLibraries(IEnumerable<Library> items)
	{
		libraries.Clear();
		foreach (var item in items)
			libraries.Add(item);
	}

	void OnItemLabelLoaded(object sender, EventArgs e)
	{
		loadedItemLabels.Add((Label)sender);
	}

	void OnItemLabelUnloaded(object sender, EventArgs e)
	{
		loadedItemLabels.Remove((Label)sender);
	}

	void OnInspectClicked(object sender, EventArgs e)
	{
		Dispatcher.Dispatch(InspectRenderedItems);
	}

	void InspectRenderedItems()
	{
		aaaCountLabel.Text = CountAttachedLabels("AAA").ToString();
		bbbCountLabel.Text = CountAttachedLabels("BBB").ToString();
		cccCountLabel.Text = CountAttachedLabels("CCC").ToString();
		inspectionGeneration++;
		inspectionGenerationLabel.Text = inspectionGeneration.ToString();
	}

	int CountAttachedLabels(string title)
	{
		return loadedItemLabels.Count(label =>
			label.Text == title &&
			label.Handler?.PlatformView is WTextBlock nativeLabel &&
			nativeLabel.IsLoaded &&
			nativeLabel.Visibility == WVisibility.Visible &&
			nativeLabel.ActualWidth > 0 &&
			nativeLabel.ActualHeight > 0 &&
			nativeLabel.XamlRoot is not null);
	}

	sealed class Library(string title, ImageSource imageSource)
	{
		public string Title { get; } = title;

		public ImageSource ImageSource { get; } = imageSource;
	}
}
#endif

