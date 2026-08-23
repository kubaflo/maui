#if WINDOWS
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34071, "[Windows] The Shell's foreground color is not applied to the ToolbarItems", PlatformAffected.UWP)]
public class Issue34071 : Shell
{
	const string AffectedToolbarItemId = "AffectedToolbarItem";
	const string ExpectedArgb = "#FF800080";

	public Issue34071()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Shell.SetForegroundColor(this, Color.FromArgb(ExpectedArgb));

		var affectedIdentity = new Label
		{
			Text = "-1",
			AutomationId = "AffectedToolbarIdentity"
		};
		var affectedForeground = new Label
		{
			Text = "-1",
			AutomationId = "AffectedToolbarForeground"
		};
		var measurementComplete = new Label
		{
			Text = "-1",
			AutomationId = "ToolbarForegroundMeasurementComplete",
			IsVisible = false
		};

		var page = new ContentPage
		{
			Title = "Home",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Expected Shell foreground: Purple",
						TextColor = Color.FromArgb(ExpectedArgb),
						FontAttributes = FontAttributes.Bold,
						HorizontalOptions = LayoutOptions.Center
					},
					new BoxView
					{
						Color = Color.FromArgb(ExpectedArgb),
						HeightRequest = 24,
						WidthRequest = 120,
						HorizontalOptions = LayoutOptions.Center
					},
					new Label
					{
						Text = "The shopping cart toolbar icon should match the purple reference.",
						HorizontalTextAlignment = TextAlignment.Center
					},
					affectedIdentity,
					affectedForeground,
					measurementComplete
				}
			}
		};
		page.ToolbarItems.Add(new ToolbarItem
		{
			IconImageSource = "shopping_cart.png",
			Order = ToolbarItemOrder.Primary,
			AutomationId = AffectedToolbarItemId
		});
		Items.Add(page);

		page.Loaded += OnPageLoaded;

		void OnPageLoaded(object sender, EventArgs args)
		{
			page.Loaded -= OnPageLoaded;
			page.Dispatcher.Dispatch(() =>
			{
				if (Handler?.PlatformView is not WFrameworkElement shellView)
					return;

				if (TryCompleteMeasurement(shellView))
					return;

				shellView.LayoutUpdated += OnLayoutUpdated;

				void OnLayoutUpdated(object layoutSender, object layoutArgs)
				{
					if (TryCompleteMeasurement(shellView))
						shellView.LayoutUpdated -= OnLayoutUpdated;
				}
			});
		}

		bool TryCompleteMeasurement(WDependencyObject shellView)
		{
			var toolbarButton = FindDescendant<WAppBarButton>(
				shellView,
				button => WAutomationProperties.GetAutomationId(button) == AffectedToolbarItemId);
			if (toolbarButton is null)
				return false;

			affectedIdentity.Text = WAutomationProperties.GetAutomationId(toolbarButton);
			affectedForeground.Text = GetArgb(toolbarButton.Foreground);
			measurementComplete.Text = "1";
			measurementComplete.IsVisible = true;
			return true;
		}
	}

	static string GetArgb(WBrush brush)
	{
		if (brush is not WSolidColorBrush solidColorBrush)
			return "UNAVAILABLE";

		var color = solidColorBrush.Color;
		return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	static T FindDescendant<T>(WDependencyObject root, Func<T, bool> predicate)
		where T : WDependencyObject
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(root);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(root, index);
			if (child is T match && predicate(match))
				return match;

			var descendant = FindDescendant(child, predicate);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}
}
#endif

