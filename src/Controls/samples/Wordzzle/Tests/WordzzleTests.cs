using Xunit;

namespace Wordzzle.Tests;

public class WordzzleTests
{
	[Theory]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void DailyPuzzleIsDeterministicAndSolvable(int length)
	{
		var date = new DateOnly(2026, 9, 29);
		var game = new WordzzleGame(length, date);
		Assert.Equal(game.Answer, new WordzzleGame(length, date).Answer);
		Assert.Equal(length, game.Answer.Length);
		Type(game, game.Answer);
		Assert.Equal(GuessResult.Won, game.SubmitGuess());
		Assert.All(game.Guesses[0].Marks, mark => Assert.Equal(LetterMark.Correct, mark));
		game.AddLetter('A');
		Assert.Empty(game.CurrentGuess);
	}

	[Fact]
	public void DuplicateLettersCannotClaimTheSameAnswerLetterTwice()
	{
		var game = new WordzzleGame(5, new DateOnly(2023, 1, 5));
		Assert.Equal("APPLE", game.Answer);
		Type(game, "SHEEP");
		Assert.Equal(GuessResult.Accepted, game.SubmitGuess());
		Assert.Equal(
			[LetterMark.Absent, LetterMark.Absent, LetterMark.Present, LetterMark.Absent, LetterMark.Present],
			game.Guesses[0].Marks);
		Assert.Equal(LetterMark.Present, game.GetKeyboardMark('E'));
	}

	[Fact]
	public void InvalidAndIncompleteGuessesDoNotConsumeAttempts()
	{
		var game = NewGame();
		Type(game, "ZZ");
		Assert.Equal(GuessResult.Incomplete, game.SubmitGuess());
		Type(game, "ZZZZ");
		Assert.Equal("ZZZZZ", game.CurrentGuess);
		Assert.Equal(GuessResult.InvalidWord, game.SubmitGuess());
		Assert.Empty(game.Guesses);
		game.RemoveLetter();
		Assert.Equal("ZZZZ", game.CurrentGuess);
	}

	[Fact]
	public void SixWrongGuessesEndTheGame()
	{
		var game = NewGame();
		for (var attempt = 1; attempt <= WordzzleGame.MaximumAttempts; attempt++)
		{
			Type(game, "WORLD");
			Assert.Equal(attempt == 6 ? GuessResult.Lost : GuessResult.Accepted, game.SubmitGuess());
		}

		Assert.True(game.IsComplete);
		Assert.False(game.IsWon);
		game.AddLetter('A');
		Assert.Empty(game.CurrentGuess);
	}

	[Fact]
	public void FullUnsubmittedGuessSurvivesRestore()
	{
		var game = NewGame();
		Type(game, "WORLD");
		game.SubmitGuess();
		Type(game, "CRANE");
		var restored = NewGame();
		Assert.True(restored.Restore(game.CreateSnapshot()));
		Assert.Single(restored.Guesses);
		Assert.Equal("CRANE", restored.CurrentGuess);
		Assert.Equal(GuessResult.Won, restored.SubmitGuess());
	}

	[Fact]
	public void InvalidRestoreDoesNotMutateExistingGame()
	{
		var game = NewGame();
		Type(game, "A");
		Assert.False(game.Restore(new GameSnapshot(5, "2023-01-01", ["WORLD", "ZZZZZ"], "")));
		Assert.Empty(game.Guesses);
		Assert.Equal("A", game.CurrentGuess);
	}

	[Fact]
	public void RestoreRejectsGuessesAfterWinAndWrongPuzzle()
	{
		var game = NewGame();
		Assert.False(game.Restore(new GameSnapshot(5, "2023-01-01", ["CRANE", "WORLD"], "")));
		Assert.False(game.Restore(new GameSnapshot(5, "2023-01-01", ["CRANE"], "A")));
		Assert.False(game.Restore(new GameSnapshot(5, "2023-01-02", [], "")));
		Assert.False(game.Restore(new GameSnapshot(4, "2023-01-01", [], "")));
		Assert.Empty(game.Guesses);
	}

	[Theory]
	[InlineData("", "Please enter your name.")]
	[InlineData("  ", "Please enter your name.")]
	[InlineData("Maui tester", "Hello, Maui tester!")]
	[InlineData(" Maui tester ", "Hello, Maui tester!")]
	public void LabValidatesNames(string name, string expected)
	{
		var state = new HackathonState { Name = name };
		state.Submit();
		Assert.Equal(expected, state.ValidationMessage);
	}

	[Fact]
	public void LabNotifiesExpressionBindings()
	{
		var state = new HackathonState();
		var changed = new List<string?>();
		state.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
		var session = state.SessionId;
		state.ClickCount++;
		state.Name = "Maui tester";
		state.Submit();
		Assert.Equal(["ClickCount", "Name", "ValidationMessage"], changed);
		Assert.Equal(session, state.SessionId);
	}

	private static WordzzleGame NewGame() => new(5, new DateOnly(2023, 1, 1));

	private static void Type(WordzzleGame game, string word)
	{
		foreach (var letter in word)
		{
			game.AddLetter(letter);
		}
	}
}
