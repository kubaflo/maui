using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27344, "PopModalAsync accesses a deleted page BindingContext", PlatformAffected.Android)]
public class Issue27344 : ContentPage
{
	const string OpenModalButtonId = "Issue27344OpenModalButton";
	const string PostDeleteReadsLabelId = "Issue27344PostDeleteReads";
	const string PersonActionButtonId = "Issue27344PersonActionButton";
	const string DeleteToolbarItemId = "Issue27344DeleteToolbarItem";

	readonly Label _postDeleteReadsLabel;

	public Issue27344()
	{
		var openModalButton = new Button
		{
			AutomationId = OpenModalButtonId,
			Text = "Open person modal"
		};
		openModalButton.Clicked += OnOpenModalClicked;

		_postDeleteReadsLabel = new Label
		{
			AutomationId = PostDeleteReadsLabelId,
			Text = "-1"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 20,
					Text = "PopModalAsync binding access"
				},
				new Label { Text = "Open the person page, then delete the person from the modal toolbar." },
				openModalButton,
				_postDeleteReadsLabel
			}
		};
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		_postDeleteReadsLabel.Text = "-1";

		var person = new PersonViewModel();
		var personActionButton = new Button
		{
			AutomationId = PersonActionButtonId,
			Text = "Person action"
		};
		BindableObject bindingTarget = personActionButton;
		BindableProperty targetProperty = Button.IsEnabledProperty;
		BindableObjectExtensions.SetBinding(bindingTarget, targetProperty, nameof(PersonViewModel.CanAct));

		var personPage = new ContentPage
		{
			Title = "Person",
			BindingContext = person,
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label { Text = "Delete this person using the toolbar." },
					personActionButton
				}
			}
		};

		var deleteItem = new ToolbarItem
		{
			AutomationId = DeleteToolbarItemId,
			Text = "Delete"
		};
		deleteItem.Clicked += async (_, _) =>
		{
			person.Delete();
			await personPage.Navigation.PopModalAsync();
			_postDeleteReadsLabel.Text = $"Completed: {person.PostDeleteCanActReads.ToString(CultureInfo.InvariantCulture)}";
		};
		personPage.ToolbarItems.Add(deleteItem);

		await Navigation.PushModalAsync(new NavigationPage(personPage));
	}

	sealed class PersonViewModel
	{
		bool _isDeleted;

		public int PostDeleteCanActReads { get; private set; }

		public bool CanAct
		{
			get
			{
				if (_isDeleted)
					PostDeleteCanActReads++;

				return !_isDeleted;
			}
		}

		public void Delete()
		{
			_isDeleted = true;
		}
	}
}

