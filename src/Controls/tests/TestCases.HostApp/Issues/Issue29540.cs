#if IOS
using Microsoft.Maui.Hosting;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29540,
	"TabbedViewHandler implementation incomplete on iOS",
	PlatformAffected.iOS)]
public class Issue29540 : NavigationPage
{
	public Issue29540() : base(new ContentPage())
	{
		Title = "TabbedViewHandler reproduction";
		var sourcePage = (ContentPage)CurrentPage;

		var navigateButton = new Button
		{
			AutomationId = "NavigateButton",
			Text = "Navigate to custom TabbedPage"
		};

		var navigationResult = new Label
		{
			AutomationId = "NavigationResult",
			Text = "Navigation pending"
		};

		navigateButton.Clicked += async (sender, args) =>
		{
			navigateButton.IsEnabled = false;

			var handler = Handler;
			if (handler is null)
			{
				throw new InvalidOperationException("The issue page must be attached before navigating.");
			}

			var mauiContext = handler.MauiContext;
			if (mauiContext is null)
			{
				throw new InvalidOperationException("The attached issue page must have a MauiContext.");
			}

			mauiContext.Handlers.GetCollection().AddHandler<SwipeTabbedPage, SwipeTabbedViewHandler>();

			try
			{
				await PushAsync(new SwipeTabbedPage());
			}
			catch (NotImplementedException exception)
			{
				System.Diagnostics.Debug.WriteLine(exception);
				navigationResult.Text = nameof(NotImplementedException);
			}
		};

		sourcePage.Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 22,
					Text = "Custom TabbedPage handler on iOS"
				},
				new Label
				{
					Text = "The action registers a TabbedViewHandler subclass for a TabbedPage subclass, then navigates to that page."
				},
				navigateButton,
				navigationResult
			}
		};
	}
}

public sealed class SwipeTabbedPage : TabbedPage
{
	public SwipeTabbedPage()
	{
		Title = "Custom tabs";
		Children.Add(new ContentPage
		{
			Title = "First",
			Content = new Label
			{
				AutomationId = "FirstTabContent",
				Text = "First tab"
			}
		});
		Children.Add(new ContentPage
		{
			Title = "Second",
			Content = new Label
			{
				Text = "Second tab"
			}
		});
	}
}

public sealed class SwipeTabbedViewHandler : Microsoft.Maui.Handlers.TabbedViewHandler
{
}
#endif

