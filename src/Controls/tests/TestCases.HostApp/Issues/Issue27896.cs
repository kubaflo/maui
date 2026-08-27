#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27896, "Android system back does not invoke the activity OnBackPressedDispatcher callback for a modal page", PlatformAffected.Android)]
public class Issue27896 : ContentPage
{
	ActivityBackCallback _registeredBackCallback = null!;
	bool _hasRegisteredBackCallback;
	int _callbackInvocationCount = -1;
	bool _modalDisappeared;

	public Issue27896()
	{
		var originalPageMarker = new Label
		{
			Text = "Android back dispatch reproduction",
			AutomationId = "Issue27896OriginalPage",
			FontAttributes = FontAttributes.Bold,
			FontSize = 22
		};

		var callbackStateLabel = new Label
		{
			Text = "Callback registered: False; callback count: -1; modal disappeared: False",
			AutomationId = "Issue27896CallbackState"
		};

		var openModalButton = new Button
		{
			Text = "Open modal page",
			AutomationId = "Issue27896OpenModal"
		};

		var rootLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				originalPageMarker,
				new Label
				{
					Text = "Open the modal page, then press the Android system back button. The activity callback should receive that back press."
				},
				callbackStateLabel,
				openModalButton
			}
		};

		openModalButton.Clicked += OpenModalPage;
		Content = rootLayout;

		async void OpenModalPage(object sender, EventArgs e)
		{
			if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not global::AndroidX.Activity.ComponentActivity activity)
			{
				callbackStateLabel.Text = "Activity unavailable";
				return;
			}

			if (_hasRegisteredBackCallback)
			{
				_registeredBackCallback.Remove();
				_registeredBackCallback.Dispose();
			}

			_callbackInvocationCount = 0;
			_modalDisappeared = false;

			var modalLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						Text = "Modal page ready",
						AutomationId = "Issue27896ModalPage"
					},
					new Label
					{
						Text = "Press the Android system back button once."
					}
				}
			};

			rootLayout.Children.Remove(callbackStateLabel);
			modalLayout.Children.Add(callbackStateLabel);

			var modalPage = new ContentPage
			{
				Content = modalLayout
			};

			_registeredBackCallback = new ActivityBackCallback(() =>
			{
				_callbackInvocationCount++;
				callbackStateLabel.Text = $"Callback registered: True; callback count: {_callbackInvocationCount}; modal disappeared: {_modalDisappeared}";
				_registeredBackCallback.Enabled = false;
				_ = modalPage.Navigation.PopModalAsync();
			});
			_hasRegisteredBackCallback = true;
			activity.OnBackPressedDispatcher.AddCallback(activity, _registeredBackCallback);
			callbackStateLabel.Text = "Callback registered: True; callback count: 0; modal disappeared: False";

			modalPage.Disappearing += OnModalPageDisappearing;
			await Navigation.PushModalAsync(modalPage);

			void OnModalPageDisappearing(object modalSender, EventArgs args)
			{
				modalPage.Disappearing -= OnModalPageDisappearing;
				_modalDisappeared = true;
				callbackStateLabel.Text = $"Modal disappeared: {_modalDisappeared}; callback count: {_callbackInvocationCount}";

				modalLayout.Children.Remove(callbackStateLabel);
				rootLayout.Children.Add(callbackStateLabel);

				_registeredBackCallback.Remove();
				_registeredBackCallback.Dispose();
				_hasRegisteredBackCallback = false;
			}
		}
	}

	sealed class ActivityBackCallback : global::AndroidX.Activity.OnBackPressedCallback
	{
		readonly Action _handleBackPressed;

		public ActivityBackCallback(Action handleBackPressed) : base(true)
		{
			_handleBackPressed = handleBackPressed;
		}

		public override void HandleOnBackPressed()
		{
			_handleBackPressed();
		}
	}
}
#endif

