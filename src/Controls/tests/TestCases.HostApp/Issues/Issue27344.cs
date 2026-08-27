namespace Maui.Controls.Sample.Issues;

#if ANDROID
[Issue(IssueTracker.Github, 27344, "PopModalAsync accesses properties on a deleted page BindingContext", PlatformAffected.Android)]
public class Issue27344 : ContentPage
{
	readonly Label _deleteReceiptLabel;
	readonly Label _postDeleteReadCountLabel;

	public Issue27344()
	{
		var openModalButton = new Button
		{
			AutomationId = "OpenModalButton",
			Text = "Open person modal"
		};
		openModalButton.Clicked += OnOpenModalClicked;

		_deleteReceiptLabel = new Label
		{
			AutomationId = "DeleteReceiptLabel",
			Text = "Delete received: 0"
		};

		_postDeleteReadCountLabel = new Label
		{
			AutomationId = "PostDeleteReadCountLabel",
			Text = "Post-delete reads: -1"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				openModalButton,
				_deleteReceiptLabel,
				_postDeleteReadCountLabel
			}
		};
	}

	async void OnOpenModalClicked(object sender, EventArgs e)
	{
		_deleteReceiptLabel.Text = "Delete received: 0";
		_postDeleteReadCountLabel.Text = "Post-delete reads: -1";
		await Navigation.PushModalAsync(new NavigationPage(new PersonPage(this)));
	}

	void CompleteDelete(int postDeleteReadCount)
	{
		_deleteReceiptLabel.Text = "Delete received: 1";
		_postDeleteReadCountLabel.Text = $"Post-delete reads: {postDeleteReadCount}";
	}

	sealed class PersonPage : ContentPage
	{
		readonly Issue27344 _owner;
		readonly PersonViewModel _personViewModel;

		internal PersonPage(Issue27344 owner)
		{
			Title = "Person";
			_owner = owner;
			_personViewModel = new PersonViewModel();
			BindingContext = _personViewModel;

			var boundButton = new Button
			{
				AutomationId = "BoundActionButton",
				Text = "Bound action"
			};
			BindableObject bindingTarget = boundButton;
			BindableProperty targetProperty = Button.IsEnabledProperty;
			BindableObjectExtensions.SetBinding(bindingTarget, targetProperty, nameof(PersonViewModel.CanDelete));

			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "Person details" },
					boundButton
				}
			};

			var deleteItem = new ToolbarItem
			{
				AutomationId = "DeleteToolbarItem",
				Text = "Delete"
			};
			deleteItem.Clicked += OnDeleteClicked;
			ToolbarItems.Add(deleteItem);
		}

		async void OnDeleteClicked(object sender, EventArgs e)
		{
			_personViewModel.Delete();
			await Navigation.PopModalAsync();
			_owner.CompleteDelete(_personViewModel.PostDeleteReadCount);
		}
	}

	sealed class PersonViewModel
	{
		bool _deleted;

		internal int PostDeleteReadCount { get; private set; }

		public bool CanDelete
		{
			get
			{
				if (_deleted)
					PostDeleteReadCount++;

				return !_deleted;
			}
		}

		internal void Delete()
		{
			_deleted = true;
		}
	}
}
#endif

