#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36412, "Done keyboard accessory blocks taps on the Entry above the keyboard", PlatformAffected.iOS)]
public class Issue36412 : ContentPage
{
	public Issue36412()
	{
		var focusEventTokenLabel = new Label
		{
			AutomationId = "Issue36412FocusEventToken",
			BackgroundColor = Colors.White,
			HorizontalOptions = LayoutOptions.End,
			InputTransparent = true,
			Text = "focusEventToken=-1",
			VerticalOptions = LayoutOptions.Start,
			ZIndex = 1,
		};

		var fields = new VerticalStackLayout
		{
			Spacing = 35,
		};

		for (var fieldNumber = 1; fieldNumber <= 15; fieldNumber++)
		{
			var focusEventToken = fieldNumber;
			var entry = new Entry
			{
				AutomationId = $"Issue36412Field{fieldNumber}",
				Keyboard = Keyboard.Numeric,
				Placeholder = $"Field {fieldNumber}",
			};

			entry.Focused += (sender, args) => focusEventTokenLabel.Text = $"focusEventToken={focusEventToken}";
			fields.Children.Add(entry);
		}

		Content = new Grid
		{
			Children =
			{
				new ScrollView { Content = fields },
				focusEventTokenLabel,
			},
		};
	}
}
#endif

