#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "[Windows] AnimationManager ObjectDisposedException IServiceProvider on closing window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	const string InitialTelemetry = "Loaded=0; PlatformViewReady=0; Destroyed=0; Dispatched=0; Attempted=0; InvocationReturned=0; Exception=None";

	public Issue34057()
	{
		var telemetryLabel = new Label
		{
			AutomationId = "Issue34057TelemetryLabel",
			Text = InitialTelemetry
		};

		var runScenarioButton = new Button
		{
			AutomationId = "Issue34057RunScenarioButton",
			Text = "Run child window close scenario"
		};

		runScenarioButton.Clicked += (_, _) =>
		{
			runScenarioButton.IsEnabled = false;

			var loaded = 0;
			var platformViewReady = 0;
			var destroyed = 0;
			var dispatched = 0;
			var attempted = 0;
			var invocationReturned = 0;
			var exceptionType = "None";

			void PublishTelemetry()
			{
				telemetryLabel.Text = $"Loaded={loaded}; PlatformViewReady={platformViewReady}; Destroyed={destroyed}; Dispatched={dispatched}; Attempted={attempted}; InvocationReturned={invocationReturned}; Exception={exceptionType}";
			}

			var savePopup = new Border
			{
				AutomationId = "Issue34057SavePopup",
				BackgroundColor = Colors.White,
				Padding = 24,
				Stroke = Colors.Gray,
				Content = new Label
				{
					Text = "Saving image...",
					FontSize = 20
				}
			};
			IAnimatable animationTarget = savePopup;

			var imageArea = new Grid
			{
				BackgroundColor = Colors.Black,
				Children =
				{
					new Label
					{
						Text = "Image viewer",
						TextColor = Colors.White,
						FontSize = 28,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					},
					savePopup
				}
			};

			var childPage = new ContentPage
			{
				Title = "Image viewer",
				Content = imageArea
			};
			var childWindow = new Window(childPage)
			{
				Title = "Image viewer"
			};

			childWindow.Destroying += (_, _) =>
			{
				destroyed++;
			};

			childPage.Loaded += (_, _) =>
			{
				loaded++;
				if (savePopup.Handler is not null && savePopup.Handler.PlatformView is not null)
				{
					platformViewReady++;
				}

				var application = Application.Current;
				if (application is null)
				{
					throw new InvalidOperationException("Application.Current was null.");
				}

				application.CloseWindow(childWindow);

				Dispatcher.Dispatch(() =>
				{
					dispatched++;
					attempted++;

					try
					{
						AnimationExtensions.Animate<double>(
							animationTarget,
							"HidePopup",
							value => 1 - value,
							opacity => savePopup.Opacity = opacity,
							rate: 16,
							length: 250);
						invocationReturned++;
					}
					catch (ObjectDisposedException)
					{
						exceptionType = nameof(ObjectDisposedException);
					}
					finally
					{
						PublishTelemetry();
					}
				});
			};

			var currentApplication = Application.Current;
			if (currentApplication is null)
			{
				throw new InvalidOperationException("Application.Current was null.");
			}

			currentApplication.OpenWindow(childWindow);
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
					Text = "Child window animation disposal",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center
				},
				telemetryLabel,
				runScenarioButton
			}
		};
	}
}
#endif

