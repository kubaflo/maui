#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26392, "Click on flyout clicks on page behind", PlatformAffected.Android)]
public class Issue26392 : Shell
{
	public Issue26392()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		var flyoutLabel = new Label
		{
			AutomationId = "Issue26392FlyoutLabel",
			Text = "MenuPage",
			Padding = 12,
			FontSize = 40,
			VerticalTextAlignment = TextAlignment.Center
		};
		var flyoutButton = new Button
		{
			Text = "Button"
		};
		var flyoutGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = 180 },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		Grid.SetRow(flyoutLabel, 0);
		Grid.SetRow(flyoutButton, 1);
		flyoutGrid.Children.Add(flyoutLabel);
		flyoutGrid.Children.Add(flyoutButton);
		FlyoutContent = flyoutGrid;

		var monkeys = new[]
		{
			"Baboon",
			"Capuchin Monkey",
			"Blue Monkey",
			"Squirrel Monkey",
			"Golden Lion Tamarin",
			"Howler Monkey",
			"Japanese Macaque"
		};
		var firstPicker = CreatePicker(monkeys, "Issue26392FirstPicker");
		var secondPicker = CreatePicker(monkeys);
		var thirdPicker = CreatePicker(monkeys);
		var fourthPicker = CreatePicker(monkeys);
		var fifthPicker = CreatePicker(monkeys);
		var focusStateLabel = new Label
		{
			AutomationId = "Issue26392FirstPickerIsFocused",
			Text = "Unmeasured"
		};
		var detailGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star },
				new RowDefinition { Height = GridLength.Auto }
			}
		};
		AddToRow(detailGrid, firstPicker, 0);
		AddToRow(detailGrid, secondPicker, 1);
		AddToRow(detailGrid, thirdPicker, 2);
		AddToRow(detailGrid, fourthPicker, 3);
		AddToRow(detailGrid, fifthPicker, 4);
		AddToRow(detailGrid, focusStateLabel, 6);

		var arranged = false;
		detailGrid.SizeChanged += (_, _) =>
		{
			if (!arranged && detailGrid.Width > 0 && detailGrid.Height > 0)
			{
				arranged = true;
				focusStateLabel.Text = firstPicker.IsFocused.ToString();
			}
		};
		firstPicker.Focused += (_, _) => focusStateLabel.Text = firstPicker.IsFocused.ToString();
		firstPicker.Unfocused += (_, _) => focusStateLabel.Text = firstPicker.IsFocused.ToString();

		Items.Add(new FlyoutItem
		{
			Title = "DetailPage",
			Items =
			{
				new ShellContent
				{
					Title = "DetailPage",
					Content = new ContentPage
					{
						Title = "DetailPage",
						Content = detailGrid
					}
				}
			}
		});
	}

	static Picker CreatePicker(string[] itemsSource, string automationId = "")
	{
		var picker = new Picker
		{
			Title = "Select a monkey",
			ItemsSource = itemsSource
		};

		if (automationId.Length > 0)
			picker.AutomationId = automationId;

		return picker;
	}

	static void AddToRow(Grid grid, View view, int row)
	{
		Grid.SetRow(view, row);
		grid.Children.Add(view);
	}
}
#endif

