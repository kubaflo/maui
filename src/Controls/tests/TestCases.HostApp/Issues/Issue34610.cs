#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34610, "Shell TitleView on iOS has unremovable horizontal margins", PlatformAffected.iOS)]
public class Issue34610 : Shell
{
	public Issue34610()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;

		var titleGrid = new Grid
		{
			AutomationId = "TitleViewGrid",
			BackgroundColor = Colors.Red,
			Padding = 0,
			Margin = 0,
			ColumnSpacing = 0,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto },
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Auto },
			},
		};

		var menuLabel = new Label
		{
			Text = "☰",
			FontSize = 24,
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(10, 0),
		};
		var titleLabel = new Label
		{
			AutomationId = "TitleText",
			Text = "MY APP TITLE",
			TextColor = Colors.White,
			FontSize = 16,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
		};
		var settingsLabel = new Label
		{
			Text = "⚙",
			FontSize = 24,
			TextColor = Colors.White,
			VerticalOptions = LayoutOptions.Center,
			Margin = new Thickness(10, 0),
		};

		Grid.SetColumn(titleLabel, 1);
		Grid.SetColumn(settingsLabel, 2);
		titleGrid.Add(menuLabel);
		titleGrid.Add(titleLabel);
		titleGrid.Add(settingsLabel);

		var statusLabel = new Label
		{
			AutomationId = "LifecycleStatus",
			Text = "Pending",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center,
		};
		var instrumentation = new VerticalStackLayout
		{
			AutomationId = "InstrumentationPanel",
			Spacing = 8,
			Padding = 12,
			BackgroundColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.End,
			Children =
			{
				statusLabel,
			},
		};
		var pageBackground = new BoxView
		{
			AutomationId = "PageBackground",
			Color = Colors.DodgerBlue,
		};
		var contentGrid = new Grid
		{
			AutomationId = "PageContent",
			Children =
			{
				pageBackground,
				instrumentation,
			},
		};
		var page = new ContentPage
		{
			Padding = 0,
			Content = contentGrid,
		};
		page.Appearing += (_, _) => statusLabel.Text = "ShellAppeared";

		SetNavBarHasShadow(page, false);
		SetTitleView(page, titleGrid);

		var shellContent = new ShellContent { Content = page };
		var shellSection = new ShellSection();
		shellSection.Items.Add(shellContent);
		var shellItem = new ShellItem();
		shellItem.Items.Add(shellSection);
		Items.Add(shellItem);
	}
}
#endif

