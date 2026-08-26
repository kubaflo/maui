namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30958, "Button remains pressed after touch is released outside its bounds", PlatformAffected.UWP)]
public class Issue30958 : ContentPage
{
	public Issue30958()
	{
		var resultLabel = new Label
		{
			AutomationId = "ResultStatus",
			Text = "No pressed-state transition observed"
		};

		var inputStatusLabel = new Label
		{
			AutomationId = "InputStatus",
			Text = "Touch input starts inside the button."
		};

		var affectedStateLabel = new Label
		{
			AutomationId = "AffectedState",
			Text = "The touch is released outside the button."
		};

		var affectedButton = new Button
		{
			AutomationId = "AffectedButton",
			Text = "Touch and drag outside",
			HorizontalOptions = LayoutOptions.Start
		};

		var pressedObserved = false;
		affectedButton.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName != Button.IsPressedProperty.PropertyName)
				return;

			if (affectedButton.IsPressed)
			{
				pressedObserved = true;
				resultLabel.Text = "Pressed";
			}
			else if (pressedObserved)
			{
				resultLabel.Text = "Released";
			}
		};

		var checkButton = new Button
		{
			AutomationId = "CheckButton",
			Text = "Check button state"
		};

		var observerLayout = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new Label
				{
					Text = "Touch and hold the button, drag outside it, then release.",
					FontAttributes = FontAttributes.Bold
				},
				resultLabel,
				inputStatusLabel,
				affectedStateLabel,
				checkButton
			}
		};

		var scrollContent = new VerticalStackLayout
		{
			Spacing = 16,
			Children =
			{
				affectedButton,
				new Label
				{
					Text = "The affected button remains visible so its default Windows pressed appearance can be observed."
				},
				new BoxView
				{
					HeightRequest = 900
				}
			}
		};

		var scrollView = new ScrollView
		{
			Content = scrollContent
		};

		var root = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};
		root.Add(observerLayout);
		root.Add(scrollView, row: 1);

		Content = root;
	}
}

