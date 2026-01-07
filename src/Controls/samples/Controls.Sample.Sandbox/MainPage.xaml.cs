using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{

		InitializeComponent();
		BindingContext = new ItemsViewModel(Result);
	}

	class Item
	{
		public string? Id { get; set; }
		public string? Text { get; set; }
		public string? Description { get; set; }
	}

	class ItemsViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<Item> Items { get; set; }
		public Command? LoadItemsCommand { get; set; }

		Item? _selectedItem;
		readonly Label _result = default!;

		public event PropertyChangedEventHandler? PropertyChanged;

		public Item? SelectedItem
		{
			get => _selectedItem;
			set { _selectedItem = value; OnPropertyChanged(); }
		}

		public Command<Item> SelectionChangedCommand { get; }

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;

			changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ItemsViewModel(Label result)
		{
			Items = new ObservableCollection<Item>();

			for (int n = 0; n < 10; n++)
			{
				Items.Add(new Item { Id = n.ToString(), Text = $"Item {n}", Description = $"This is item {n}" });
			}

			SelectionChangedCommand = new Command<Item>(item =>
			{
				var fromParameter = item;
				var fromSelectedItem = SelectedItem;

				if (fromParameter != fromSelectedItem)
				{
					_result.Text = "Fail";
				}
				else
				{
					_result.Text = "Success";
				}
				_ = Application.Current!.Windows[0]!.Page!.Navigation!.PushAsync(new Subpage1());
			});
			_result = result ?? throw new ArgumentNullException(nameof(result));
		}
	}

	async void OnNavigateToSubpage1(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new Subpage1());
	}

	async void OnNavigateToSubpage2(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new Subpage1());
	}

	void OnItemTapped(object sender, TappedEventArgs e)
	{
		Navigation.PushAsync(new Subpage1());
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		AccessibilityFocusStore.RestoreFocus();
	}

	protected override async void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
		if (args.NavigationType == NavigationType.Pop)
		{
			AccessibilityFocusStore.RestoreFocus();
		}
	}


	class Subpage1 : ContentPage
	{
		public Subpage1()
		{
			Title = "Subpage 1";
			Content = new StackLayout
			{
				Children =
				{
					new Label { Text = "This is Subpage 1" },
					new Button
					{
						Text = "Go to Subpage 2",
						Command = new Command(async () =>
						{
							await Navigation.PushAsync(new Subpage2());
						})
					}
				}
			};
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			AccessibilityFocusStore.RestoreFocus();
		}

		protected override void OnNavigatedTo(NavigatedToEventArgs args)
		{
			base.OnNavigatedTo(args);
			if (args.NavigationType == NavigationType.Pop)
			{
				AccessibilityFocusStore.RestoreFocus();
			}
		}


		class Subpage2 : ContentPage
		{
			public Subpage2()
			{
				Title = "Subpage 2";
				Content = new Label { Text = "This is Subpage 2" };
			}


		}
	}

}