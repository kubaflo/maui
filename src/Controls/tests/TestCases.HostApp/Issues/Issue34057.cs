#if WINDOWS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34057, "[Windows] AnimationManager ObjectDisposedException when closing a window", PlatformAffected.UWP)]
public class Issue34057 : ContentPage
{
	public Issue34057()
	{
		var resultLabel = new Label
		{
			AutomationId = "Issue34057ResultLabel",
			Text = "NOT_RUN",
			FontAttributes = FontAttributes.Bold,
			HorizontalTextAlignment = TextAlignment.Center
		};

		var completionLabel = new Label
		{
			AutomationId = "Issue34057CompletionLabel",
			Text = "NOT_COMPLETED",
			IsVisible = false,
			HorizontalTextAlignment = TextAlignment.Center
		};

		var openChildWindowButton = new Button
		{
			AutomationId = "Issue34057OpenChildWindowButton",
			Text = "Open child window",
			IsEnabled = true
		};

		openChildWindowButton.Clicked += (_, _) =>
		{
			openChildWindowButton.IsEnabled = false;

			var popupLoaded = false;
			var sameWindowDestroying = false;
			var postDestructionCallbackRan = false;
			var animationAttempted = false;
			var animationException = "not-observed";

			var editorSurface = new Grid
			{
				BackgroundColor = Colors.DimGray,
				Padding = 24
			};

			editorSurface.Add(new Label
			{
				Text = "Image editor",
				TextColor = Colors.White,
				FontSize = 28,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			});

			var savePopup = new Border
			{
				AutomationId = "Issue34057SavePopup",
				BackgroundColor = Colors.White,
				Padding = 20,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.End,
				Content = new Label
				{
					Text = "Save popup",
					TextColor = Colors.Black,
					FontAttributes = FontAttributes.Bold
				}
			};
			editorSurface.Add(savePopup);

			var childWindow = new Window(new ContentPage
			{
				Title = "Image viewer",
				Content = editorSurface
			});

			var application = Application.Current
				?? throw new InvalidOperationException("The test requires a running MAUI application.");

			savePopup.Loaded += (_, _) =>
			{
				popupLoaded = true;
				Dispatcher.Dispatch(() => application.CloseWindow(childWindow));
			};

			childWindow.Destroying += (sender, _) =>
			{
				sameWindowDestroying = ReferenceEquals(sender, childWindow);
				Dispatcher.Dispatch(AnimateAfterWindowDestruction);
			};

			application.OpenWindow(childWindow);

			void AnimateAfterWindowDestruction()
			{
				postDestructionCallbackRan = true;
				animationAttempted = true;
				IAnimatable popupAnimationTarget = savePopup;

				try
				{
					AnimationExtensions.Animate(
						popupAnimationTarget,
						"HidePopup",
						value => savePopup.Opacity = value,
						1,
						0,
						length: 250);
					animationException = "none";
				}
				catch (ObjectDisposedException exception)
				{
					animationException = $"{exception.GetType().Name}:{exception.ObjectName}";
				}

				resultLabel.Text =
					$"popupLoaded={popupLoaded};windowDestroying={(sameWindowDestroying ? "same" : "different")};" +
					$"postDestructionCallback={postDestructionCallbackRan};animationAttempted={animationAttempted};" +
					$"exception={animationException}";
				completionLabel.Text = "Child window close completed";
				completionLabel.IsVisible = true;
			}
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Child window animation disposal",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center
				},
				new Label
				{
					Text = "Open a child image editor window. The visible save popup starts hiding as that window closes.",
					HorizontalTextAlignment = TextAlignment.Center
				},
				openChildWindowButton,
				resultLabel,
				completionLabel
			}
		};
	}
}
#endif

