namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyQuiz")]
internal class NeedyQuiz() : ModuleScript<KtaneDefuserConnector.Components.NeedyQuiz>(2, 1) {
	public override string IndefiniteDescription => "a Needy Quiz";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private bool _lastAnswerWasAboutPrevious;
	private bool _lastAnswer;
	private bool _everAnsweredNo;
	private bool _everAnsweredYes;
	private bool _everAnsweredIncorrectly;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(10);
			using var interrupt = await ModuleInterruptAsync(context);
			var data = interrupt.Read(Reader);
			bool answer;
			switch (data.Message) {
				case null: return;
				case "Abort?": answer = false; _lastAnswerWasAboutPrevious = false; break;
				case "Are you a dirty cheater?": answer = false; _lastAnswerWasAboutPrevious = false; break;
				case "Do you have at least 1 strike?": answer = GameState.Current.Strikes >= 1; _lastAnswerWasAboutPrevious = false; break;
				case "Do you have less than 1 strike?": answer = GameState.Current.Strikes < 1; _lastAnswerWasAboutPrevious = false; break;
				case "Do you have more than 1 strike?": answer = GameState.Current.Strikes > 1; _lastAnswerWasAboutPrevious = false; break;
				case "Do you have up to 1 strike?": answer = GameState.Current.Strikes <= 1; _lastAnswerWasAboutPrevious = false; break;
				case "Do you have more strikes than batteries?": answer = GameState.Current.Strikes > GameState.Current.BatteryCount; _lastAnswerWasAboutPrevious = false; break;
				case "Does the parity of batteries matth the parity of serial number digits?": answer = (GameState.Current.BatteryCount + GameState.Current.SerialNumber.Count(char.IsDigit)) % 2 == 0; _lastAnswerWasAboutPrevious = false; break;
				case "Does the serial contain duplicate characters?": answer = SerialNumberContainsDuplicateCharacters(); _lastAnswerWasAboutPrevious = false; break;
				case "Does this question contain six lines?": answer = false; _lastAnswerWasAboutPrevious = false; break;
				case "Does this question contain six words?": answer = true; _lastAnswerWasAboutPrevious = false; break;
				case "Does this question contain three lines?": answer = true; _lastAnswerWasAboutPrevious = false; break;
				case "Does this question contain three words?": answer = false; _lastAnswerWasAboutPrevious = false; break;
				case "Have you previously answered No to a question?": answer = _everAnsweredNo; _lastAnswerWasAboutPrevious = true; break;
				case "Have you previously answered Yes to a question?": answer = _everAnsweredYes; _lastAnswerWasAboutPrevious = true; break;
				case "Have you previously answered a question incorrectly?": answer = _everAnsweredIncorrectly; _lastAnswerWasAboutPrevious = true; break;
				case "Have you previously answered not No to a question?": answer = _everAnsweredYes; _lastAnswerWasAboutPrevious = true; break;
				case "Have you previously answered not Yes to a question?": answer = _everAnsweredNo; _lastAnswerWasAboutPrevious = true; break;
				case "Was the last answered question about a previous question or answer?": answer = _lastAnswerWasAboutPrevious; _lastAnswerWasAboutPrevious = true; break;
				case "What was your previous answer?": answer = _lastAnswer; _lastAnswerWasAboutPrevious = true; break;
				case "What wasn't your previous answer?": answer = !_lastAnswer; _lastAnswerWasAboutPrevious = true; break;
				case { } when data.Message.StartsWith("SEGFAULT"): answer = true; _lastAnswerWasAboutPrevious = false; break;
				default: throw new InvalidOperationException($"Unknown question: {data.Message}");
			}

			_lastAnswer = answer;
			_everAnsweredNo |= !answer;
			_everAnsweredYes |= answer;
			Interact(interrupt, answer ? 0 : 1, 0);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	protected internal override void Strike(AimlAsyncContext context) {
		if (NeedyState == NeedyState.Running)
			_everAnsweredIncorrectly = true;
	}

	private static bool SerialNumberContainsDuplicateCharacters() {
		var chars = new HashSet<char>();
		foreach (var c in GameState.Current.SerialNumber) {
			if (!chars.Add(c)) return true;
		}
		return false;
	}
}
