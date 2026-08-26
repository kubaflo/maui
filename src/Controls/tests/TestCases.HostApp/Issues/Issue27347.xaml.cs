using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27347, "MultiBinding converters are not triggered on ObservableCollection changes", PlatformAffected.iOS)]
public partial class Issue27347 : ContentPage
{
	ObservableCollection<string> _attachments = [];
	bool _isEditable;

	public Issue27347()
	{
		InitializeComponent();
		BindingContext = this;
		_attachments.CollectionChanged += OnAttachmentsChanged;
	}

	public ObservableCollection<string> Attachments
	{
		get => _attachments;
		private set
		{
			if (_attachments == value)
				return;

			_attachments.CollectionChanged -= OnAttachmentsChanged;
			_attachments = value;
			_attachments.CollectionChanged += OnAttachmentsChanged;
			OnPropertyChanged();
		}
	}

	public bool IsEditable
	{
		get => _isEditable;
		private set
		{
			if (_isEditable == value)
				return;

			_isEditable = value;
			OnPropertyChanged();
		}
	}

	void OnToggleDataClicked(object sender, EventArgs e)
	{
		Attachments = new ObservableCollection<string>(["Attachment 1", "Attachment 2"]);
	}

	void OnToggleEditModeClicked(object sender, EventArgs e)
	{
		IsEditable = !IsEditable;
	}

	void OnDeleteClicked(object sender, EventArgs e)
	{
		if (sender is Button { CommandParameter: string attachment })
			Attachments.Remove(attachment);
	}

	void OnAttachmentsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		CollectionCountLabel.Text = Attachments.Count.ToString(CultureInfo.InvariantCulture);
	}
}

public sealed class Issue27347IsListNotNullOrNotEmptyConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is IEnumerable items && items.Cast<object>().Any();

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

public sealed class Issue27347IsMultiValueAllTrueConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values.Length > 0 && values.All(value => value is true);

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
