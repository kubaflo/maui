#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26502, "WindowManagerFlags.Secure does not block screenshots on modal pages", PlatformAffected.Android)]
public class Issue26502 : ContentPage
{
	const string PendingStatus = "Pending";
	readonly Label _activityStatusLabel;

	public Issue26502()
	{
		Title = "Secure main page";

		var openModalButton = new Button
		{
			Text = "Open modal page",
			AutomationId = "OpenModalPage",
		};
		openModalButton.Clicked += OnOpenModalClicked;

		_activityStatusLabel = new Label
		{
			Text = PendingStatus,
			AutomationId = "ActivitySecureStatus",
			HorizontalOptions = LayoutOptions.Center,
		};

		var mainLayout = CreateLayout();
		mainLayout.Add(new Label
		{
			Text = "Main page is protected",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
		});
		mainLayout.Add(openModalButton, 0, 1);
		mainLayout.Add(_activityStatusLabel, 0, 2);
		Content = mainLayout;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window is not global::Android.Views.Window activityWindow)
		{
			_activityStatusLabel.Text = "ActivityWindow=False;Secure=False";
			return;
		}

		activityWindow.SetFlags(
			global::Android.Views.WindowManagerFlags.Secure,
			global::Android.Views.WindowManagerFlags.Secure);

		var isSecure = HasSecureFlag(activityWindow);
		_activityStatusLabel.Text = $"ActivityWindow=True;Secure={isSecure}";
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		var modalStatusLabel = new Label
		{
			Text = PendingStatus,
			AutomationId = "ModalSecureStatus",
			HorizontalOptions = LayoutOptions.Center,
		};

		var modalLayout = CreateLayout();
		modalLayout.Add(new Label
		{
			Text = "Modal page",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
		});
		modalLayout.Add(new Label
		{
			Text = "Modal page is visible",
			AutomationId = "ModalPageContent",
			HorizontalOptions = LayoutOptions.Center,
		}, 0, 1);
		modalLayout.Add(modalStatusLabel, 0, 2);

		var modalPage = new ContentPage
		{
			Title = "Modal page",
			Content = modalLayout,
		};
		modalPage.Loaded += (_, _) => InspectModalWindow(modalPage, modalStatusLabel);
		modalPage.Disappearing += (_, _) => ClearSecureFlag();

		await Navigation.PushModalAsync(modalPage);
	}

	static Grid CreateLayout() => new()
	{
		Padding = 24,
		RowDefinitions =
		{
			new RowDefinition(GridLength.Auto),
			new RowDefinition(GridLength.Auto),
			new RowDefinition(GridLength.Auto),
		},
		RowSpacing = 20,
		VerticalOptions = LayoutOptions.Center,
	};

	static void InspectModalWindow(ContentPage modalPage, Label statusLabel)
	{
		if (modalPage.Handler?.PlatformView is not global::Android.Views.View platformView)
		{
			statusLabel.Text = "Loaded=True;Handler=False;Dialog=False;Distinct=False;Secure=False";
			return;
		}

		if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not global::AndroidX.Fragment.App.FragmentActivity activity ||
			activity.Window is not global::Android.Views.Window activityWindow)
		{
			statusLabel.Text = "Loaded=True;Handler=True;Dialog=False;Distinct=False;Secure=False";
			return;
		}

		foreach (var fragment in activity.SupportFragmentManager.Fragments)
		{
			if (fragment is not global::AndroidX.Fragment.App.DialogFragment dialogFragment ||
				dialogFragment.View is not global::Android.Views.View fragmentView ||
				!ContainsView(fragmentView, platformView) ||
				dialogFragment.Dialog?.Window is not global::Android.Views.Window dialogWindow)
			{
				continue;
			}

			var isDistinct = !ReferenceEquals(activityWindow, dialogWindow);
			var isSecure = HasSecureFlag(dialogWindow);
			statusLabel.Text = $"Loaded=True;Handler=True;Dialog=True;Distinct={isDistinct};Secure={isSecure}";
			return;
		}

		statusLabel.Text = "Loaded=True;Handler=True;Dialog=False;Distinct=False;Secure=False";
	}

	static bool ContainsView(global::Android.Views.View root, global::Android.Views.View target)
	{
		if (ReferenceEquals(root, target))
		{
			return true;
		}

		if (root is not global::Android.Views.ViewGroup viewGroup)
		{
			return false;
		}

		for (var index = 0; index < viewGroup.ChildCount; index++)
		{
			var child = viewGroup.GetChildAt(index);
			if (child is not null && ContainsView(child, target))
			{
				return true;
			}
		}

		return false;
	}

	static bool HasSecureFlag(global::Android.Views.Window window)
	{
		var attributes = window.Attributes;
		return attributes is not null &&
			(attributes.Flags & global::Android.Views.WindowManagerFlags.Secure) == global::Android.Views.WindowManagerFlags.Secure;
	}

	static void ClearSecureFlag()
	{
		if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window is global::Android.Views.Window activityWindow)
		{
			activityWindow.ClearFlags(global::Android.Views.WindowManagerFlags.Secure);
		}
	}
}
#endif

