#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31039, "Entry gains focus when an InputTransparent Entry is clicked inside a ScrollView", PlatformAffected.UWP)]
public class Issue31039 : ContentPage
{
	public Issue31039()
	{
		var focusedCount = 0;
		var focusedCountLabel = new Label
		{
			AutomationId = "FirstEntryFocusedCount",
			Text = "FirstEntry Focused Count: 0"
		};

		var firstEntry = new Entry
		{
			AutomationId = "FirstEntry",
			Placeholder = "First focusable Entry"
		};
		firstEntry.Focused += (sender, args) =>
		{
			focusedCount++;
			focusedCountLabel.Text = $"FirstEntry Focused Count: {focusedCount}";
		};

		var transparentEntry = new Entry
		{
			AutomationId = "TransparentEntry",
			Placeholder = "InputTransparent Entry",
			InputTransparent = true
		};

		var lastEntry = new Entry
		{
			AutomationId = "LastEntry",
			Placeholder = "Another focusable Entry"
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					focusedCountLabel,
					firstEntry,
					transparentEntry,
					lastEntry
				}
			}
		};
	}
}
#endif

