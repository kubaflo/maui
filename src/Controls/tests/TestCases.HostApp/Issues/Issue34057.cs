#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "AnimationManager ObjectDisposedException when closing a child window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	public Issue34057()
	{
		var openChildWindowButton = new Button
		{
			AutomationId = "Issue34057OpenChildWindow",
			Text = "Open child window"
		};

		var resultButton = new Button
		{
			AutomationId = "Issue34057Result",
			Text = "NotTriggered",
			InputTransparent = true
		};

		var lifecycleCompleteLabel = new Label
		{
			AutomationId = "Issue34057LifecycleComplete",
			Text = "Pending",
			IsVisible = false,
			HorizontalTextAlignment = TextAlignment.Center
		};

		openChildWindowButton.Clicked += (_, _) =>
		{
			var app = Application.Current;
			if (app is null)
				throw new InvalidOperationException("Application.Current must be available to open the child window.");

			resultButton.Text = "NotTriggered";
			lifecycleCompleteLabel.Text = "Pending";
			lifecycleCompleteLabel.IsVisible = false;
			openChildWindowButton.IsEnabled = false;

			var loadedOccurred = false;
			var disappearingOccurred = false;
			var dispatchExecuted = false;
			var closeReturned = false;
			var exceptionState = "NotTriggered";

			var editorSurface = new Grid
			{
				HeightRequest = 360,
				BackgroundColor = Colors.DarkSlateGray,
				Children =
				{
					new Label
					{
						Text = "Image editor surface",
						TextColor = Colors.White,
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					}
				}
			};

			var savePopup = new Border
			{
				BackgroundColor = Colors.White,
				Padding = 20,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Content = new Label
				{
					Text = "Save popup",
					FontSize = 22
				}
			};
			IAnimatable animationTarget = savePopup;

			var childPage = new ContentPage
			{
				Title = "Image viewer",
				Content = new Grid
				{
					Padding = 24,
					Children =
					{
						editorSurface,
						savePopup
					}
				}
			};

			var childWindow = new Window(childPage)
			{
				Title = "Image viewer"
			};

			childPage.Disappearing += (_, _) =>
			{
				disappearingOccurred = true;
				Dispatcher.Dispatch(() =>
				{
					dispatchExecuted = true;
					exceptionState = "None";

					try
					{
						AnimationExtensions.Animate(
							animationTarget,
							"HidePopup",
							value => savePopup.Opacity = 1 - value,
							16,
							250);
					}
					catch (ObjectDisposedException exception)
					{
						exceptionState = $"ObjectDisposedException({exception.ObjectName})";
					}

					CompleteIfReady();
				});
			};

			childPage.Loaded += (_, _) =>
			{
				if (loadedOccurred)
					return;

				loadedOccurred = true;
				app.CloseWindow(childWindow);
				closeReturned = true;
				CompleteIfReady();
			};

			app.OpenWindow(childWindow);

			void CompleteIfReady()
			{
				if (!closeReturned || !dispatchExecuted)
					return;

				var windowRemoved = true;
				foreach (var openWindow in app.Windows)
				{
					if (ReferenceEquals(openWindow, childWindow))
					{
						windowRemoved = false;
						break;
					}
				}

				resultButton.Text = exceptionState;
				lifecycleCompleteLabel.Text =
					$"Complete: Loaded={loadedOccurred}; Disappearing={disappearingOccurred}; WindowRemoved={windowRemoved}; Dispatch={dispatchExecuted}";
				lifecycleCompleteLabel.IsVisible = true;
				openChildWindowButton.IsEnabled = true;
			}
		};

		Content = new VerticalStackLayout
		{
			Padding = 32,
			Spacing = 20,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Issue 34057: close a child image-editor window while its save popup is visible",
					FontSize = 20,
					HorizontalTextAlignment = TextAlignment.Center
				},
				openChildWindowButton,
				resultButton,
				lifecycleCompleteLabel
			}
		};
	}
}
#endif

