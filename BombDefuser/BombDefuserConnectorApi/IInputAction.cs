using System;

namespace BombDefuserConnectorApi;
public interface IInputAction { }

/// <summary>An action that does nothing.</summary>
public sealed class NoOpAction : IInputAction { }

/// <summary>An action that presses, starts holding or releases a controller button.</summary>
public sealed class ButtonAction : IInputAction {
	public Button Button { get; set; }
	public ButtonActionType Action { get; set; }

	public ButtonAction(Button button) : this(button, ButtonActionType.Press) { }
	public ButtonAction(Button button, ButtonActionType action) {
		this.Button = button;
		this.Action = action;
	}
}

/// <summary>An action that moves a controller stick or trigger axis.</summary>
public sealed class AxisAction : IInputAction {
	public Axis Axis { get; set; }
	public float Value { get; set; }

	public AxisAction(Axis axis, float value) {
		this.Axis = axis;
		this.Value = value;
	}
}

/// <summary>An action that changes the camera's field of view, as if using the Camera Zoom mod.</summary>
public sealed class ZoomAction : IInputAction {
	public float Value { get; set; }

	public ZoomAction(float value) => this.Value = value;
}

/// <summary>An action that sends a callback event to the client when reached.</summary>
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
