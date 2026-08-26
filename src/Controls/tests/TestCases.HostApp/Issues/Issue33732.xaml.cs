using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33732, "Repeated MakeVisible moves an already-visible CollectionView item", PlatformAffected.UWP)]
public partial class Issue33732 : ContentPage
{
	int _requestCount;

	public Issue33732()
	{
		InitializeComponent();

		PositionPicker.ItemsSource = new[]
		{
			nameof(ScrollToPosition.MakeVisible),
			nameof(ScrollToPosition.Start),
			nameof(ScrollToPosition.Center),
			nameof(ScrollToPosition.End)
		};
		PositionPicker.SelectedIndex = 0;
		BindingContext = this;
		ConfigurationLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"Position={PositionPicker.SelectedItem}; Animate={AnimateSwitch.IsToggled}; Density={DeviceDisplay.MainDisplayInfo.Density}");
	}

	public IReadOnlyList<Issue33732MonkeyItem> Monkeys { get; } =
	[
		new("Baboon", "Africa & Asia"),
		new("Capuchin Monkey", "Central & South America"),
		new("Blue Monkey", "Central and East Africa"),
		new("Squirrel Monkey", "Central & South America"),
		new("Golden Lion Tamarin", "Brazil"),
		new("Howler Monkey", "South America"),
		new("Japanese Macaque", "Japan"),
		new("Mandrill", "Southern Cameroon, Gabon, Equatorial Guinea, and Congo"),
		new("Proboscis Monkey", "Borneo"),
		new("Red-shanked Douc", "Vietnam, Laos"),
		new("Gray-shanked Douc", "Vietnam"),
		new("Golden Snub-nosed Monkey", "China"),
		new("Black Snub-nosed Monkey", "China"),
		new("Tonkin Snub-nosed Monkey", "Vietnam"),
		new("Thomas's Langur", "Indonesia"),
		new("Purple-faced Langur", "Sri Lanka"),
		new("Gelada", "Ethiopia")
	];

	void OnScrollButtonClicked(object sender, EventArgs e)
	{
		_requestCount++;
		RequestCountLabel.Text = $"Requests={_requestCount}";

		MonkeyCollection.ScrollTo(
			Monkeys[8],
			position: (ScrollToPosition)PositionPicker.SelectedIndex,
			animate: AnimateSwitch.IsToggled);
	}

	void OnMonkeyRowLoaded(object sender, EventArgs e)
	{
		if (sender is Grid { BindingContext: Issue33732MonkeyItem { Name: "Proboscis Monkey" } } targetRow &&
			string.IsNullOrEmpty(targetRow.AutomationId))
		{
			targetRow.AutomationId = "TargetRow";
		}
	}
}

public sealed class Issue33732MonkeyItem(string name, string location)
{
	public string Name { get; } = name;
	public string Location { get; } = location;
	public string ImageSource { get; } = "dotnet_bot.svg";
}
