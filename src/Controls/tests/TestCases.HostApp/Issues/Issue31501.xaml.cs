using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31501, "CollectionView header binding does not update after replacing its source", PlatformAffected.Android)]
public partial class Issue31501 : ContentPage
{
	const string UpdatedValue = "After tap";

	readonly Issue31501ViewModel _viewModel = new();
	Label _headerValueLabel;

	public Issue31501()
	{
		InitializeComponent();
		BindingContext = _viewModel;
	}

	void OnReplaceDataClicked(object sender, EventArgs e)
	{
		_viewModel.Data = new Data
		{
			HasData = true,
			StringValue = UpdatedValue
		};

		Dispatcher.Dispatch(() => TriggerState.Text = $"Observed:{_headerValueLabel.Text}");
	}

	void OnHeaderValueLabelLoaded(object sender, EventArgs e)
	{
		_headerValueLabel = (Label)sender;
	}
}

public sealed class Issue31501ViewModel : INotifyPropertyChanged
{
	Data _data = new()
	{
		HasData = true,
		StringValue = "Before tap"
	};

	public Data Data
	{
		get => _data;
		set
		{
			if (_data == value)
				return;

			_data = value;
			PropertyChanged(this, new PropertyChangedEventArgs(nameof(Data)));
		}
	}

	public bool IsVisible => true;

	public event PropertyChangedEventHandler PropertyChanged = delegate { };
}

public sealed class Data
{
	public bool HasData { get; init; }

	public string StringValue { get; init; } = string.Empty;
}
