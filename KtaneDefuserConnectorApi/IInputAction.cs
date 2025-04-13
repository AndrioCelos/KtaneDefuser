using System;

namespace KtaneDefuserConnectorApi;
public interface IInputAction { }

/// <summary>An action that does nothing.</summary>
public sealed class NoOpAction : IInputAction { }

/// <summary>An action that presses, starts holding or releases a controller button.</summary>
public sealed class ButtonAction(Button button, ButtonActionType action = ButtonActionType.Press) : IInputAction {
	public Button Button { get; set; } = button;
	public ButtonActionType Action { get; set; } = action;
}

/// <summary>An action that moves a controller stick or trigger axis.</summary>
public sealed class AxisAction(Axis axis, float value) : IInputAction {
	public Axis Axis { get; set; } = axis;
	public float Value { get; set; } = value;
}

/// <summary>An action that changes the camera's field of view, as if using the Camera Zoom mod.</summary>
public sealed class ZoomAction(float value) : IInputAction {
	public float Value { get; set; } = value;
}

/// <summary>An action that sends a callback event to the client when reached.</summary>
public sealed class CallbackAction(Guid token) : IInputAction {
	public Guid Token { get; set; } = token;
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
