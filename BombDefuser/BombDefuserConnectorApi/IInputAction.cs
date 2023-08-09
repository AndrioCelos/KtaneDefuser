using System;

namespace BombDefuserConnectorApi;
public interface IInputAction { }

public sealed class NoOpAction : IInputAction { }

public sealed class ButtonAction : IInputAction {
	public Button Button { get; set; }
	public ButtonActionType Action { get; set; }

	public ButtonAction(Button button) : this(button, ButtonActionType.Press) { }
	public ButtonAction(Button button, ButtonActionType action) {
		this.Button = button;
		this.Action = action;
	}
}

public sealed class AxisAction : IInputAction {
	public Axis Axis { get; set; }
	public float Value { get; set; }

	public AxisAction(Axis axis, float value) {
		this.Axis = axis;
		this.Value = value;
	}
}

public sealed class ZoomAction : IInputAction {
	public float Value { get; set; }

	public ZoomAction(float value) => this.Value = value;
}

public sealed class CallbackAction : IInputAction {
	public Guid Token { get; set; }

	public CallbackAction(Guid token) => this.Token = token;
}

public enum InputActionType {
	None,
	Button,
	Axis,
	Zoom,
	Callback
}

public enum ButtonActionType {
	Press,
	Hold,
	Release
}

public enum Button {
	A,
	B,
	X,
	Y,
	Left,
	Right,
	Up,
	Down,
	LB,
	RB,
	Start
}

public enum Axis {
	LeftStickX,
	LeftStickY,
	RightStickX,
	RightStickY,
	LT,
	RT
}
