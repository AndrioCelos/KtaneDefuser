using System.Web;
using AngelAiml.Media;
using Button = KtaneDefuserConnectorApi.Button;

namespace KtaneDefuserScripts;
public static class Utils {
	public static Quadrilateral CurrentModuleArea { get; } = new( 836,  390, 1120,  390,  832,  678, 1124,  678);
	private static readonly Quadrilateral[] ModuleAreas = [
		new( 220,  100,  496,  100,  193,  359,  479,  359),
		new( 535,  100,  806,  101,  522,  359,  801,  360),
		new( 840,  101, 1113,  101,  836,  360, 1119,  360),
		new(1147,  101, 1407,  101, 1154,  360, 1421,  360),
		new(1456,  102, 1718,  102, 1474,  360, 1745,  360),
		new( 190,  392,  477,  392,  160,  678,  459,  678),
		new( 520,  392,  800,  392,  501,  677,  794,  677),
		new( 836,  390, 1124,  390,  836,  678, 1124,  678),
		new(1155,  390, 1425,  390, 1163,  676, 1442,  676),
		new(1476,  390, 1748,  390, 1497,  676, 1779,  676),
		new( 157,  706,  457,  705,  124, 1019,  436, 1018),
		new( 501,  705,  794,  705,  481, 1018,  787, 1017),
		new( 829,  705, 1125,  704,  828, 1018, 1134, 1016),
		new(1164,  704, 1444,  704, 1173, 1016, 1465, 1015),
		new(1499,  704, 1782,  703, 1521, 1015, 1816, 1014)
	];
	private static readonly Quadrilateral[] BombAreas = [
		new( 572,  291,  821,  291,  559,  534,  816,  534),
		new( 852,  291, 1096,  291,  848,  534, 1101,  534),
		new(1127,  292, 1369,  292, 1134,  533, 1383,  533),
		new( 558,  558,  816,  558,  544,  822,  811,  822),
		new( 848,  558, 1099,  558,  845,  821, 1106,  821),
		new(1134,  558, 1385,  558, 1141,  821, 1400,  821)
	];
	internal static readonly Quadrilateral[] SideWidgetAreas = [
		new( 813,  465,  817,  228,  988,  465,  988,  228),
		new( 988,  465,  988,  228, 1163,  465, 1158,  228),
		new( 808,  772,  812,  515,  988,  772,  988,  515),
		new( 988,  772,  988,  515, 1168,  772, 1164,  515)
	];
	internal static readonly Quadrilateral[] TopBottomWidgetAreas = [
		new( 588,  430,  784,  430,  587,  541,  784,  541),
		new( 824,  430, 1140,  430,  824,  541, 1140,  541),
		new(1181,  430, 1389,  430, 1182,  540, 1390,  541),
		new( 580,  566,  783,  566,  578,  678,  782,  678),
		new( 821,  566, 1140,  566,  821,  678, 1140,  678),
		new(1181,  566, 1390,  566, 1182,  678, 1392,  678)
	];

	/// <summary>Returns the quadrilateral representing the location of the specified slot on the screen. The slot should be on the side of the bomb we're already looking at.</summary>
	public static Quadrilateral GetPoints(Slot slot) {
		if (GameState.Current.BombType == BombType.Centurion) return CenturionUtil.GetPoints(slot);
		if (GameState.Current.FocusState == FocusState.Module) {
			var selectedSlot = GameState.Current.SelectedFace.SelectedSlot;
			var dx = slot.X - selectedSlot.X;
			var dy = slot.Y - selectedSlot.Y;
			return ModuleAreas[(dy + 1) * 5 + dx + 2];
		} else
			return BombAreas[slot.Y * 3 + slot.X];
	}

	public static void AddReply(this AimlAsyncContext context, Reply reply) => AddReply(context, reply.Text, reply.Postback);
	public static void AddReply(this AimlAsyncContext context, string text) => context.Reply($"<reply>{HttpUtility.HtmlEncode(text)}</reply>");
	public static void AddReply(this AimlAsyncContext context, string text, string postback) => context.Reply($"<reply><text>{HttpUtility.HtmlEncode(text)}</text><postback>{HttpUtility.HtmlEncode(postback)}</postback></reply>");
	public static void AddReplies(this AimlAsyncContext context, params string[] replies) {
		foreach (var text in replies) AddReply(context, text);
	}
	public static void AddReplies(this AimlAsyncContext context, params Reply[] replies) => AddReplies(context, (IEnumerable<Reply>) replies);
	public static void AddReplies(this AimlAsyncContext context, IEnumerable<Reply> replies) {
		foreach (var reply in replies) AddReply(context, reply);
	}

	/// <summary>Returns the number represented by the specified ordinal word.</summary>
	public static int ParseOrdinal(string ordinal) => (int) Enum.Parse<Ordinal>(ordinal, true);
	/// <summary>Returns an ordinal word representing the specified number.</summary>
	public static string ToOrdinal(int n) => $"{n}{(n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" }}";

	/// <summary>Selects the specified bomb face, optionally looking at widgets on the intermediate side face.</summary>
	internal static async Task SelectFaceAsync(Interrupt interrupt, int face, SelectFaceAlignMode alignMode) {
		if (GameState.Current.SelectedFaceNum == face) return;  // The requested side is already selected; do nothing.
		GameState.Current.LookingAtSide = true;
		interrupt.SendInputs(new AxisAction(Axis.RightStickX, 1));
		if (alignMode == SelectFaceAlignMode.CheckWidgets) {
			await Delay(0.375);
			interrupt.SendInputs(new AxisAction(Axis.RightStickX, 0));
			using var ss = DefuserConnector.Instance.TakeScreenshot();
			Edgework.RegisterWidgets(interrupt.Context, true, ss);
			interrupt.SendInputs(new AxisAction(Axis.RightStickX, 1));
			await Delay(0.125);
		} else
			await Delay(0.75);

		GameState.Current.FocusState = FocusState.Bomb;
		GameState.Current.SelectedFaceNum = face;
		GameState.Current.SelectedModuleNum = null;

		// Correct the bomb rotation.
		if (alignMode == SelectFaceAlignMode.None)
			interrupt.SendInputs(new AxisAction(Axis.RightStickX, 0));
		else {
			// If the face we're moving to is known to have modules on it, pressing A B is faster.
			if (GameState.Current.SelectedFace.HasModules) {
				interrupt.SendInputs(new AxisAction(Axis.RightStickX, 0), new ButtonAction(Button.A), new ButtonAction(Button.B));
				await Delay(0.5);
			} else {
				interrupt.SendInputs(new AxisAction(Axis.RightStickX, 0), new ButtonAction(Button.B), new ButtonAction(Button.A));
				await Delay(1.5);
			}
		}
		GameState.Current.LookingAtSide = false;
	}

	/// <summary>Selects the specified module.</summary>
	/// <param name="interrupt">The interrupt to use for sending inputs.</param>
	/// <param name="moduleNum">The index of the module to select.</param>
	/// <param name="waitForFocus">If true, waits for the module focusing animation to finish before completing; otherwise completes as soon as controller input can be sent to the module.</param>
	internal static async Task SelectModuleAsync(Interrupt interrupt, int moduleNum, bool waitForFocus) {
		if (GameState.Current.SelectedModuleNum == moduleNum) return;  // The requested module is already selected.
		var slot = GameState.Current.Modules[moduleNum].Slot;
		await SelectSlotAsync(interrupt, slot, waitForFocus);
		GameState.Current.SelectedModuleNum = moduleNum;
	}

	internal static async Task SelectSlotAsync(Interrupt interrupt, Slot slot, bool waitForFocus) {
		var buttons = new List<Button>();
		await SelectFaceAsync(interrupt, slot.Face, SelectFaceAlignMode.None);
		// If another module is selected, deselect it first.
		if (GameState.Current.SelectedModuleNum is not null)
			buttons.Add(Button.B);
		// Move to the correct row.
		var currentSlot = GameState.Current.SelectedFace.SelectedSlot;
		if (slot.Y != currentSlot.Y) {
			buttons.Add(slot.Y < currentSlot.Y ? Button.Up : Button.Down);

			// Find which slot this input will select.
			currentSlot.Y = slot.Y;
			if (GameState.Current.SelectedFace[currentSlot]?.Reader is null or KtaneDefuserConnector.Components.Timer) {
				for (var d = 0; d < 2; d++) {
					var x2 = currentSlot.X + (d == 0 ? -1 : 1);
					if (x2 is < 0 or >= 3 || GameState.Current.SelectedFace[x2, currentSlot.Y]?.Reader is null or KtaneDefuserConnector.Components.Timer) continue;
					currentSlot.X = x2;
					break;
				}
			}
		}
		// Move to the correct module within that row.
		while (slot.X < currentSlot.X) {
			buttons.Add(Button.Left);
			do { currentSlot.X--; } while (GameState.Current.SelectedFace[currentSlot]?.Reader is null or KtaneDefuserConnector.Components.Timer);
		}
		while (slot.X > currentSlot.X) {
			buttons.Add(Button.Right);
			do { currentSlot.X++; } while (GameState.Current.SelectedFace[currentSlot]?.Reader is null or KtaneDefuserConnector.Components.Timer);
		}
		buttons.Add(Button.A);
		await interrupt.SendInputsAsync(buttons);
		if (waitForFocus) await Delay(0.75);

		GameState.Current.SelectedFace.SelectedSlot = slot;
		GameState.Current.FocusState = FocusState.Module;
	}

	/// <summary>Returns a value indicating whether the specified module is currently visible to the bot.</summary>
	internal static bool CanReadModuleImmediately(int moduleIndex) => !GameState.Current.LookingAtSide && GameState.Current.Modules[moduleIndex].Slot.Face == GameState.Current.SelectedFaceNum;

	internal static IEnumerable<T[]> EnumeratePermutations<T>(IEnumerable<T> items) {
		var array = items.ToArray();
		return EnumeratePermutationsInternal(array, array.Length);
		
		static IEnumerable<T[]> EnumeratePermutationsInternal(T[] array, int length) {
			// Use Heap's algorithm to generate permutations.
			if (length == 1) {
				yield return array;
				yield break;
			}

			foreach (var a in EnumeratePermutationsInternal(array, length - 1)) yield return a;
			for (var i = 0; i < length - 1; i++) {
				if (length % 2 == 0)
					(array[i], array[length - 1]) = (array[length - 1], array[i]);
				else
					(array[0], array[length - 1]) = (array[length - 1], array[0]);
				foreach (var a in EnumeratePermutationsInternal(array, length - 1)) yield return a;
			}
		}
	}
}

[AimlSet]
public enum Ordinal {
	[AimlSetItem("zeroth"), AimlSetItem("0th")]
	Zeroth,
	[AimlSetItem("first"), AimlSetItem("1st")]
	First,
	[AimlSetItem("second"), AimlSetItem("2nd")]
	Second,
	[AimlSetItem("third"), AimlSetItem("3rd")]
	Third,
	[AimlSetItem("fourth"), AimlSetItem("4th")]
	Fourth,
	[AimlSetItem("fifth"), AimlSetItem("5th")]
	Fifth,
	[AimlSetItem("sixth"), AimlSetItem("6th")]
	Sixth,
	[AimlSetItem("seventh"), AimlSetItem("7th")]
	Seventh,
	[AimlSetItem("eighth"), AimlSetItem("8th")]
	Eighth,
	[AimlSetItem("ninth"), AimlSetItem("9th")]
	Ninth,
	[AimlSetItem("tenth"), AimlSetItem("10th")]
	Tenth
}
