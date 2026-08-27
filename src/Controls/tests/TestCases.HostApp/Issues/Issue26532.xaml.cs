using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26532, "Changing BindingContext clears the previous Picker selection", PlatformAffected.Android)]
public partial class Issue26532 : ContentPage
{
	readonly QuestionViewModel _firstQuestion;
	readonly QuestionViewModel _secondQuestion;
	bool _firstQuestionWasAnswered;

	public Issue26532()
	{
		InitializeComponent();

		_firstQuestion = new QuestionViewModel(
			new[] { "Answer A", "Answer B" },
			OnFirstQuestionSelectionChanged);
		_secondQuestion = new QuestionViewModel(
			Array.Empty<string>(),
			_ => { });

		BindingContext = _firstQuestion;
	}

	void OnFirstQuestionSelectionChanged(string selectedAnswer)
	{
		OriginalSelectionLabel.Text = $"Original selection: {selectedAnswer ?? "<null>"}";

		if (selectedAnswer is not null)
		{
			_firstQuestionWasAnswered = true;
		}
		else if (_firstQuestionWasAnswered)
		{
			DefectStatusLabel.Text = "Previous selection was cleared";
		}
	}

	void OnNextClicked(object sender, EventArgs e)
	{
		BindingContext = _secondQuestion;
		ContextStateLabel.Text = "Second question active";
		NextButton.IsEnabled = false;
	}

	sealed class QuestionViewModel : INotifyPropertyChanged
	{
		readonly Action<string> _selectionChanged;
		string _selectedAnswer = null!;

		public QuestionViewModel(IEnumerable<string> answers, Action<string> selectionChanged)
		{
			Answers = new ObservableCollection<string>(answers);
			_selectionChanged = selectionChanged;
		}

		public ObservableCollection<string> Answers { get; }

		public string SelectedAnswer
		{
			get => _selectedAnswer;
			set
			{
				if (_selectedAnswer == value)
					return;

				_selectedAnswer = value;
				_selectionChanged(value);
				PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedAnswer)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}
}
