using SixLabors.ImageSharp;

namespace KtaneDefuserScripts;

public class SelectableHandler(int selectableWidth, int selectableHeight, int defaultSelectionX = 0, int defaultSelectionY = 0) {
	/// <summary>Returns or sets the size of the object's selectable grid.</summary>
	public Size SelectableSize { get; set; } = new(selectableWidth, selectableHeight);

	/// <summary>Returns or sets the location in the object's selectable grid of the current selection.</summary>
	public Point Selection { get; set; } = new(defaultSelectionX, defaultSelectionY);

	/// <summary>Returns a value indicating whether there is a child object in the specified slot in the selectable grid.</summary>
	protected virtual bool IsSelectablePresent(int x, int y) => true;

	/// <summary>Moves the selection to the specified slot.</summary>
	public void Select(Interrupt interrupt, int x, int y) {
		var inputs = GetSelectInputs(x, y);
		if (inputs.Count == 0) return;
		interrupt.SendInputs(inputs);
	}

	/// <summary>Moves the selection to the specified slot and waits for the inputs to complete.</summary>
	public async Task SelectAndWaitAsync(Interrupt interrupt, int x, int y) {
		var inputs = GetSelectInputs(x, y);
		if (inputs.Count == 0) return;
		await interrupt.SendInputsAsync(inputs);
	}

	/// <summary>Interacts with the object in the specified slot.</summary>
	public void Interact(Interrupt interrupt, int x, int y) {
		var inputs = GetSelectInputs(x, y);
		interrupt.SendInputs(inputs.Append(Button.A));
	}

	/// <summary>Interacts with the object in the specified slot and waits for the inputs to complete.</summary>
	public async Task InteractWaitAsync(Interrupt interrupt, int x, int y) {
		var inputs = GetSelectInputs(x, y);
		await interrupt.SendInputsAsync(inputs.Append(Button.A));
	}

	/// <summary>Moves the selection to the specified slot and returns the sequence of buttons to press.</summary>
	public List<Button> GetSelectInputs(int x, int y) {
		ArgumentOutOfRangeException.ThrowIfNegative(x);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, SelectableSize.Width);
		ArgumentOutOfRangeException.ThrowIfNegative(y);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, SelectableSize.Height);
		if (!IsSelectablePresent(x, y))
			throw new ArgumentException("Cannot select an empty slot.");
		if (Selection.X == x && Selection.Y == y)
			return [];
		
		var buttons = new List<Button>();

		// Try to move vertically first.
		var currentSlot = Selection;
		while (true) {
			if (currentSlot.Y == y) {
				// Move horizontally within the correct row.
				while (x < currentSlot.X) {
					currentSlot = MoveHighlight(currentSlot, Button.Left);
					buttons.Add(Button.Left);
				}
				while (x > currentSlot.X) {
					currentSlot = MoveHighlight(currentSlot, Button.Right);
					buttons.Add(Button.Right);
				}
				if (currentSlot.X != x) throw new InvalidOperationException("Could not reach the specified slot?!");
				Selection = currentSlot;
				return buttons;
			}

			var button = currentSlot.Y > y ? Button.Up : Button.Down;
			currentSlot = MoveHighlight(currentSlot, button); // This should always result in movement.
			buttons.Add(button);

			if (button == Button.Up ? currentSlot.Y < y : currentSlot.Y > y) {
				// Overshoot; must move horizontally first instead.
				break;
			}
		}

		// Try to move horizontally first.
		buttons.Clear();
		currentSlot = Selection;
		while (true) {
			if (currentSlot.X == x) {
				// Move vertically within the correct column.
				while (y < currentSlot.Y) {
					currentSlot = MoveHighlight(currentSlot, Button.Up);
					buttons.Add(Button.Up);
				}
				while (y > currentSlot.Y) {
					currentSlot = MoveHighlight(currentSlot, Button.Down);
					buttons.Add(Button.Down);
				}
				if (currentSlot.Y != y) throw new InvalidOperationException("Could not reach the specified slot?!");
				Selection = currentSlot;
				return buttons;
			}

			var button = currentSlot.X > x ? Button.Left : Button.Right;
			currentSlot = MoveHighlight(currentSlot, button);  // This should always result in movement.
			buttons.Add(button);

			if (button == Button.Left ? currentSlot.Y < y : currentSlot.Y > y) {
				// Overshoot; must move horizontally first instead.
				throw new InvalidOperationException("No way to select the specified slot.");
			}
		}
	}

	private Point MoveHighlight(Point selection, Button button) {
		var size = Math.Max(SelectableSize.Width, SelectableSize.Height);
		for (var side = 0; side < size; side++) {
			for (var forward = 1; forward < size; forward++) {
				var (x1, y1, x2, y2) = button switch {
					Button.Up => (selection.X - side, selection.Y - forward, selection.X + side, selection.Y - forward),
					Button.Right => (selection.X + forward, selection.Y - side, selection.X + forward, selection.Y + side),
					Button.Down => (selection.X - side, selection.Y + forward, selection.X + side, selection.Y + forward),
					_ => (selection.X - forward, selection.Y - side, selection.X - forward, selection.Y + side)
				};
				if (x1 >= 0 && x1 < SelectableSize.Width && y1 >= 0 && y1 < SelectableSize.Height && IsSelectablePresent(x1, y1))
					return new(x1, y1);
				if (x2 >= 0 && x2 < SelectableSize.Width && y2 >= 0 && y2 < SelectableSize.Height && IsSelectablePresent(x2, y2))
					return new(x2, y2);
			}
		}
		return selection;
	}
}
