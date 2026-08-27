#if ANDROID
using System.ComponentModel;
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27344, "PopModalAsync accesses properties on the removed page's BindingContext", PlatformAffected.Android)]
public class Issue27344 : ContentPage
{
	const string NotCompleted = "NotCompleted";
	readonly Label _completionLabel;
	readonly PersonViewModel _person;
	readonly Label _postDeleteReadCountLabel;

	public Issue27344()
	{
		_person = new PersonViewModel();
		_postDeleteReadCountLabel = new Label
		{
			AutomationId = "Issue27344PostDeleteReadCount",
			Text = "-1"
		};
		_completionLabel = new Label
		{
			AutomationId = "Issue27344Completion",
			Text = NotCompleted
		};

		var openButton = new Button
		{
			AutomationId = "Issue27344OpenButton",
			Text = "Open person modal"
		};
		openButton.Clicked += OnOpenModalClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				openButton,
				_postDeleteReadCountLabel,
				_completionLabel
			}
		};
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		_person.Reset();
		_postDeleteReadCountLabel.Text = "-1";
		_completionLabel.Text = NotCompleted;

		var actionButton = new Button
		{
			AutomationId = "Issue27344BoundAction",
			Text = "Bound action"
		};
		BindableObject bindingTarget = actionButton;
		BindableProperty bindingTargetProperty = Button.IsEnabledProperty;

		// This public API creates and applies the BindingExpression with binding specificity.
		// Removing the modal then reaches that binding through IPropertyPropagationController.
		BindableObjectExtensions.SetBinding(
			bindingTarget,
			bindingTargetProperty,
			nameof(PersonViewModel.CanPerformAction));

		var preDeleteReadCountLabel = new Label
		{
			AutomationId = "Issue27344PreDeleteReadCount",
			Text = _person.PostDeleteReadCount.ToString(CultureInfo.InvariantCulture)
		};
		var personPage = new ContentPage
		{
			Title = "Person",
			BindingContext = _person,
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label { Text = "Person modal" },
					actionButton,
					preDeleteReadCountLabel
				}
			}
		};

		var deleteItem = new ToolbarItem
		{
			AutomationId = "Issue27344Delete",
			Text = "Delete"
		};
		deleteItem.Clicked += async (deleteSender, deleteArgs) =>
		{
			_person.Delete();
			await personPage.Navigation.PopModalAsync(false);
			_postDeleteReadCountLabel.Text = _person.PostDeleteReadCount.ToString(CultureInfo.InvariantCulture);
			_completionLabel.Text = "ModalPopped";
		};
		personPage.ToolbarItems.Add(deleteItem);

		await Navigation.PushModalAsync(new NavigationPage(personPage), false);
	}

	sealed class PersonViewModel : INotifyPropertyChanged
	{
		bool _isDeleted;

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		public int PostDeleteReadCount { get; private set; }

		public bool CanPerformAction
		{
			get
			{
				if (_isDeleted)
					PostDeleteReadCount++;

				return !_isDeleted;
			}
		}

		public void Delete()
		{
			_isDeleted = true;
		}

		public void Reset()
		{
			_isDeleted = false;
			PostDeleteReadCount = 0;
			PropertyChanged(this, new PropertyChangedEventArgs(nameof(CanPerformAction)));
		}
	}
}
#endif

