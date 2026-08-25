#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30990, "Shell toolbar ignores shell properties", PlatformAffected.Android)]
public class Issue30990 : Shell
{
	public const string IconToolbarItemId = "Issue30990IconToolbarItem";
	public const string StatusLabelId = "Issue30990StatusLabel";
	public const string TextToolbarItemId = "Issue30990TextToolbarItem";

	public Issue30990()
	{
		Resources.Add(new Style(typeof(Shell))
		{
			ApplyToDerivedTypes = true,
			Setters =
			{
				new Setter
				{
					Property = Shell.ForegroundColorProperty,
					Value = Colors.Red
				},
				new Setter
				{
					Property = Shell.TitleColorProperty,
					Value = Colors.Red
				}
			}
		});

		const string groceriesSource = "groceries.png";
		var statusLabel = new Label
		{
			AutomationId = StatusLabelId,
			Text = "NOT_LOADED"
		};
		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Children =
				{
					new Label
					{
						Text = "Hello",
						TextColor = Colors.White
					},
					statusLabel
				}
			},
			ToolbarItems =
			{
				new ToolbarItem
				{
					AutomationId = TextToolbarItemId,
					Text = "Text 1"
				},
				new ToolbarItem
				{
					AutomationId = IconToolbarItemId,
					IconImageSource = groceriesSource
				}
			}
		};

		page.Loaded += (_, _) =>
		{
			var foregroundColor = Shell.GetForegroundColor(this);
			foregroundColor.ToRgba(out byte red, out byte green, out byte blue, out byte alpha);
			statusLabel.Text = $"LOADED:#{red:X2}{green:X2}{blue:X2}{alpha:X2}|{groceriesSource}";
		};

		Items.Add(new ShellContent { Content = page });
	}
}
#endif

