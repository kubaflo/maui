using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 14793, "[Android] Toolbar's text style for default theme has too low contrast", PlatformAffected.Android)]
	public class Issue14793NavPage : NavigationPage
	{
		public Issue14793NavPage() : base(new Issue14793()) { }

		public class Issue14793 : ContentPage
		{
			public Issue14793()
			{
				Title = "Title";
				Content = new VerticalStackLayout()
				{
					new Label()
					{
						Text = "Hello, World!",
						AutomationId = "label"
					}
				};
			}
		}
	}
}
