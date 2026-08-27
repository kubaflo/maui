#if ANDROID
using AndroidX.Activity;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27896, "Android Back does not invoke an activity OnBackPressedDispatcher callback when dismissing a modal page", PlatformAffected.Android)]
public class Issue27896 : ContentPage
{
	readonly Label _callbackStateLabel;
	readonly Button _openModalButton;

#if ANDROID
	bool _activityBackReceived;
#endif

	public Issue27896()
	{
		Title = "Android modal Back dispatcher";

		_callbackStateLabel = new Label
		{
			Text = "Activity back callback: waiting",
			AutomationId = "CallbackStateLabel"
		};

		_openModalButton = new Button
		{
			Text = "Open modal page",
			AutomationId = "OpenModalButton"
		};

#if ANDROID
		_openModalButton.Clicked += OnOpenModalClicked;
#endif

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					Text = "Android modal Back dispatcher",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				},
				new Label
				{
					Text = "The activity callback should receive Android Back before the modal page is dismissed."
				},
				_callbackStateLabel,
				_openModalButton
			}
		};
	}

#if ANDROID
	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		_activityBackReceived = false;
		_callbackStateLabel.Text = "Activity back callback: waiting";
		_openModalButton.IsEnabled = false;

		if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not ComponentActivity activity)
		{
			_callbackStateLabel.Text = "Activity back callback: unavailable";
			_openModalButton.IsEnabled = true;
			return;
		}

		var application = Application.Current;
		if (application is null || application.Windows.Count == 0)
		{
			_callbackStateLabel.Text = "Activity back callback: unavailable";
			_openModalButton.IsEnabled = true;
			return;
		}

		var backPressedCallback = new RecordingBackPressedCallback(this, activity.OnBackPressedDispatcher);
		activity.OnBackPressedDispatcher.AddCallback(activity, backPressedCallback);

		var modalPage = new ContentPage
		{
			Title = "Modal page",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						Text = "Modal page ready",
						AutomationId = "ModalReadyLabel",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Press Android Back once"
					}
				}
			}
		};

		modalPage.Disappearing += OnModalDisappearing;
		await application.Windows[0].Navigation.PushModalAsync(modalPage);

		void OnModalDisappearing(object disappearingSender, EventArgs args)
		{
			modalPage.Disappearing -= OnModalDisappearing;
			_callbackStateLabel.Text = _activityBackReceived
				? "Activity back callback: received"
				: "Activity back callback: not received";
			_openModalButton.IsEnabled = true;
			backPressedCallback.Remove();
			backPressedCallback.Dispose();
		}
	}

	sealed class RecordingBackPressedCallback : OnBackPressedCallback
	{
		readonly Issue27896 _page;
		readonly OnBackPressedDispatcher _dispatcher;

		public RecordingBackPressedCallback(Issue27896 page, OnBackPressedDispatcher dispatcher)
			: base(true)
		{
			_page = page;
			_dispatcher = dispatcher;
		}

		public override void HandleOnBackPressed()
		{
			_page._activityBackReceived = true;
			_page._callbackStateLabel.Text = "Activity back callback: received";
			Enabled = false;
			_dispatcher.OnBackPressed();
		}
	}
#endif
}

