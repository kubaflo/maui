using System.ComponentModel;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26532, "Picker clears the previous BindingContext selection", PlatformAffected.Android)]
public partial class Issue26532 : ContentPage
{
	readonly QuestionViewModel _firstQuestion;
	readonly QuestionViewModel _secondQuestion;

	public Issue26532()
	{
		InitializeComponent();

		_firstQuestion = new QuestionViewModel(
			[new Answer("First answer"), new Answer("Second answer")],
			OnFirstQuestionSelectedAnswerChanged);
		_secondQuestion = new QuestionViewModel([], _ => { });
		BindingContext = _firstQuestion;
	}

	void OnFirstQuestionSelectedAnswerChanged(Answer answer)
	{
		OriginalSelectionLabel.Text = $"Original item selected answer: {answer?.Text ?? "<null>"}";
	}

	void NextButton_Clicked(object sender, EventArgs e)
	{
		BindingContext = _secondQuestion;
		ReplacementAnswerCountLabel.Text = $"Replacement answer count: {_secondQuestion.Answers.Count}";
		TransitionStatusLabel.Text = "BindingContext changed to empty question";
	}

	sealed class QuestionViewModel : INotifyPropertyChanged
	{
		readonly Action<Answer> _selectedAnswerChanged;
		Answer _selectedAnswer = null!;

		public QuestionViewModel(IReadOnlyList<Answer> answers, Action<Answer> selectedAnswerChanged)
		{
			Answers = answers;
			_selectedAnswerChanged = selectedAnswerChanged;
		}

		public IReadOnlyList<Answer> Answers { get; }

		public Answer SelectedAnswer
		{
			get => _selectedAnswer;
			set
			{
				if (_selectedAnswer == value)
					return;

				_selectedAnswer = value;
				PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedAnswer)));
				_selectedAnswerChanged(value);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}

	sealed class Answer
	{
		public Answer(string text)
		{
			Text = text;
		}

		public string Text { get; }

		public override string ToString() => Text;
	}
}
