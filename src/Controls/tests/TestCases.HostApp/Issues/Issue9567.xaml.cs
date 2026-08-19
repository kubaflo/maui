using System.Collections.ObjectModel;

namespace Maui.Controls.Sample.Issues;

#if IOS
[Issue(IssueTracker.Github, 9567, "CollectionView SelectionChanged is not raised when tapping a button in an item", PlatformAffected.iOS)]
#endif
public partial class Issue9567 : ContentPage
{
	int _selectionChangedCallbackCount = -1;

	public Issue9567()
	{
		InitializeComponent();
		BindingContext = new Issue9567ViewModel(OnItemRemoved);
	}

	void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectionChangedCallbackCount = _selectionChangedCallbackCount < 0
			? 1
			: _selectionChangedCallbackCount + 1;
		SelectionCallbackCountLabel.Text = _selectionChangedCallbackCount.ToString();
		SelectedIdentityLabel.Text = e.CurrentSelection.FirstOrDefault() is Issue9567Item item
			? item.Name
			: "none";
	}

	void OnItemRemoved()
	{
		ItemCountLabel.Text = $"Issue9567 item count: {((Issue9567ViewModel)BindingContext).Data.Count}";
	}
}

public sealed class Issue9567ViewModel
{
	readonly Action _itemRemoved;

	public Issue9567ViewModel(Action itemRemoved)
	{
		_itemRemoved = itemRemoved;
		Data =
		[
			new("model_1", "Make1", "Issue9567Model1", "Issue9567DeleteModel1"),
			new("model_2", "Make2", "Issue9567Model2", "Issue9567DeleteModel2"),
			new("model_3", "Make3", "Issue9567Model3", "Issue9567DeleteModel3"),
			new("model_4", "Make4", "Issue9567Model4", "Issue9567DeleteModel4")
		];
		RemoveEquipmentCommand = new Command<Issue9567Item>(RemoveItem);
	}

	public ObservableCollection<Issue9567Item> Data { get; }

	public Command<Issue9567Item> RemoveEquipmentCommand { get; }

	void RemoveItem(Issue9567Item item)
	{
		Data.Remove(item);
		_itemRemoved();
	}
}

public sealed class Issue9567Item
{
	public Issue9567Item(string name, string make, string itemAutomationId, string deleteAutomationId)
	{
		Name = name;
		Car = new Issue9567Vehicle(make);
		ItemAutomationId = itemAutomationId;
		DeleteAutomationId = deleteAutomationId;
	}

	public string Name { get; }

	public Issue9567Vehicle Car { get; }

	public string ItemAutomationId { get; }

	public string DeleteAutomationId { get; }
}

public sealed class Issue9567Vehicle
{
	public Issue9567Vehicle(string make)
	{
		Make = make;
	}

	public string Make { get; }
}
