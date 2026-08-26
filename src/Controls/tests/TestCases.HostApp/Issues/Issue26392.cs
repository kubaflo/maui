namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26392, "Click on flyout clicks on page behind", PlatformAffected.Android)]
public class Issue26392 : Shell
{
	const string CallbackSentinel = "FlyoutIsPresented callback: sentinel";
	const string CallbackPresented = "FlyoutIsPresented callback: True";

	public Issue26392()
	{
		FlyoutBehavior = FlyoutBehavior.Flyout;

		var flyoutGrid = new Grid
		{
			AutomationId = "Issue26392MenuPage",
			RowDefinitions =
			{
				new RowDefinition { Height = 64 },
				new RowDefinition { Height = 64 },
				new RowDefinition { Height = 64 },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		flyoutGrid.Add(new Label
		{
			Text = "MenuPage",
			VerticalTextAlignment = TextAlignment.Center
		});

		flyoutGrid.Add(new Button
		{
			Text = "Flyout button"
		}, 0, 1);

		flyoutGrid.Add(new BoxView
		{
			AutomationId = "Issue26392FlyoutBlank",
			Color = Colors.Transparent
		}, 0, 2);

		FlyoutContent = flyoutGrid;

		string[] monkeys = ["Baboon", "Capuchin", "Blue Monkey"];
		var callbackLabel = new Label
		{
			AutomationId = "Issue26392FlyoutCallback",
			Text = CallbackSentinel,
			VerticalTextAlignment = TextAlignment.Center
		};

		var pageGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = 48 },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		pageGrid.Add(new Label
		{
			AutomationId = "Issue26392PickerSource",
			Text = $"Picker source: {string.Join(", ", monkeys)}",
			VerticalTextAlignment = TextAlignment.Center
		});
		pageGrid.Add(callbackLabel, 0, 1);

		for (int row = 2; row <= 6; row++)
		{
			var picker = new Picker
			{
				Title = "Select a monkey",
				ItemsSource = monkeys
			};

			if (row == 2)
				picker.AutomationId = "Issue26392FirstPicker";

			pageGrid.Add(picker, 0, row);
		}

		var contentPage = new ContentPage
		{
			Title = "Issue 26392",
			Content = pageGrid
		};

		var flyoutItem = new FlyoutItem { Title = "Home" };
		flyoutItem.Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "Issue26392Page",
			ContentTemplate = new DataTemplate(() => contentPage)
		});
		Items.Add(flyoutItem);

		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == FlyoutIsPresentedProperty.PropertyName && FlyoutIsPresented)
				callbackLabel.Text = CallbackPresented;
		};
	}
}

