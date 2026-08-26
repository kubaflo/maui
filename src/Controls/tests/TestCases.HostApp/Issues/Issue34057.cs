#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	public Issue34057()
	{
		var loadedCount = 0;
		var destroyingCount = 0;
		var continuationCount = 0;
		var triggerRunning = false;

		var loadedCountLabel = new Label
		{
			AutomationId = "LoadedCountLabel",
			Text = "Loaded callbacks: 0"
		};
		var destroyingCountLabel = new Label
		{
			AutomationId = "DestroyingCountLabel",
			Text = "Destroying callbacks: 0"
		};
		var continuationCountLabel = new Label
		{
			AutomationId = "ContinuationCountLabel",
			Text = "Continuation callbacks: 0"
		};
		var animationStateLabel = new Label
		{
			AutomationId = "AnimationStateLabel",
			Text = "Animation state: NotStarted"
		};
		var triggerButton = new Button
		{
			AutomationId = "RunTriggerButton",
			Text = "Run child-window close trigger"
		};

		triggerButton.Clicked += (_, _) =>
		{
			if (triggerRunning)
				return;

			var app = Application.Current ?? throw new InvalidOperationException("The test application is unavailable.");
			triggerRunning = true;
			triggerButton.IsEnabled = false;

			var popup = new Border
			{
				BackgroundColor = Colors.White,
				Padding = 20,
				Stroke = Colors.Gray,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Content = new VerticalStackLayout
				{
					Spacing = 12,
					Children =
					{
						new Label { FontSize = 22, Text = "Save image" },
						new Label { Text = "Choose a format to save the edited image." },
						new Button { Text = "Save" }
					}
				}
			};

			var childPage = new ContentPage
			{
				Title = "Image editor",
				Content = new Grid
				{
					BackgroundColor = Colors.LightGray,
					Children =
					{
						new Label
						{
							FontSize = 28,
							HorizontalOptions = LayoutOptions.Center,
							Text = "Image editor surface",
							VerticalOptions = LayoutOptions.Center
						},
						popup
					}
				}
			};
			var childWindow = new Window(childPage)
			{
				Title = "Image editor"
			};
			IAnimatable animationTarget = popup;
			var originalDispatcher = Dispatcher;

			childWindow.Destroying += (_, _) =>
			{
				destroyingCount++;
				destroyingCountLabel.Text = $"Destroying callbacks: {destroyingCount}";

				originalDispatcher.Dispatch(() =>
				{
					continuationCount++;
					continuationCountLabel.Text = $"Continuation callbacks: {continuationCount}";

					try
					{
						AnimationExtensions.Animate<double>(
							animationTarget,
							"HidePopup",
							static value => value,
							value => popup.Opacity = 1 - value);
						animationStateLabel.Text = "Animation state: AnimateReturned";
					}
					catch (ObjectDisposedException)
					{
						animationStateLabel.Text = "Animation state: ObjectDisposedException: IServiceProvider";
					}
					finally
					{
						triggerRunning = false;
						triggerButton.IsEnabled = true;
					}
				});
			};

			childPage.Loaded += (_, _) =>
			{
				loadedCount++;
				loadedCountLabel.Text = $"Loaded callbacks: {loadedCount}";
				childPage.Dispatcher.Dispatch(() => app.CloseWindow(childWindow));
			};

			app.OpenWindow(childWindow);
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					FontSize = 24,
					Text = "Child window popup animation"
				},
				new Label
				{
					Text = "The child window contains an image-editor surface and a visible save popup. It closes after loading, then the queued popup animation continues."
				},
				triggerButton,
				loadedCountLabel,
				destroyingCountLabel,
				continuationCountLabel,
				animationStateLabel
			}
		};
	}
}
#endif

