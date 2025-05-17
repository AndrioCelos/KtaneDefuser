using System;
using System.Collections.Generic;
using System.Text;

namespace KtaneDefuserConnectorApi;
/// <summary>A base interface for defuser interface messages.</summary>
public interface IDefuserMessage {
	MessageType MessageType { get; }
	int ToBuffer(byte[] buffer);
}

[Obsolete("This message type is being replaced by specific Command messages.")]
public struct LegacyCommandMessage(string command) : IDefuserMessage {
	public string Command = command ?? throw new ArgumentNullException(nameof(command));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(Command, 0, Command.Length, buffer, 5);
}

[Obsolete("This message type is being replaced by specific event messages.")]
public struct LegacyEventMessage(string eventMessage) : IDefuserMessage {
	public string Event = eventMessage ?? throw new ArgumentNullException(nameof(eventMessage));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyEvent;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(Event, 0, Event.Length, buffer, 5);
}

/// <summary>The response to a <see cref="ScreenshotCommandMessage"/>.</summary>
public struct ScreenshotResponseMessage(int pixelDataLength, byte[] data) : IDefuserMessage {
	/// <summary>The length of the pixel data in bytes.</summary>
	public int PixelDataLength = pixelDataLength;
	/// <summary>The buffer containing the image width, height and pixel data.</summary>
	public byte[] Data = data ?? throw new ArgumentNullException(nameof(data));
	/// <summary>Returns the pixel width of the image.</summary>
	public readonly unsafe int Width {
		get {
			fixed (byte* ptr = Data)
				return *(int*) ptr;
		}
	}
	/// <summary>Returns the pixel height of the image.</summary>
	public readonly unsafe int Height {
		get {
			fixed (byte* ptr = Data)
				return *(int*) (ptr + 4);
		}
	}

	readonly MessageType IDefuserMessage.MessageType => MessageType.ScreenshotResponse;
	public readonly int ToBuffer(byte[] buffer) => PixelDataLength + 8;  // The caller should handle encoding the image data.
}

/// <summary>The response to a <see cref="CheatReadCommandMessage"/>.</summary>
public struct CheatReadResponseMessage(string? data) : IDefuserMessage {
	/// <summary>A string representation of the data read.</summary>
	public string? Data = data;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadResponse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Data is not null ? Encoding.UTF8.GetBytes(Data, 0, Data.Length, buffer, 5) : 0;
}

/// <summary>The response to a <see cref="CheatReadCommandMessage"/> if an error occurs.</summary>
public struct CheatReadErrorMessage(string message) : IDefuserMessage {
	public string Message = message ?? throw new ArgumentNullException(nameof(message));

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadError;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(Message, 0, Message.Length, buffer, 5);
}

[Obsolete($"This message is being replaced with {nameof(InputCallbackMessage)}")]
public struct LegacyInputCallbackMessage(string token) : IDefuserMessage {
	public string Token = token ?? throw new ArgumentNullException(nameof(token));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyInputCallback;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(Token, 0, Token.Length, buffer, 5);
}

/// <summary>A command that sends controller inputs to the game.</summary>
public struct InputCommandMessage(IEnumerable<IInputAction> actions) : IDefuserMessage {
	public IEnumerable<IInputAction> Actions = actions ?? throw new ArgumentNullException(nameof(actions));

	readonly MessageType IDefuserMessage.MessageType => MessageType.InputCommand;
	public readonly unsafe int ToBuffer(byte[] buffer) {
		var length = 0;
		fixed (byte* ptr = buffer) {
			foreach (var action in Actions) {
				var ptr2 = ptr + 5;
				switch (action) {
					case NoOpAction:
						*(ptr2 + length) = (int) InputActionType.None;
						length++;
						break;
					case ButtonAction buttonAction:
						*(ptr2 + length) = (int) InputActionType.Button;
						*(ptr2 + length + 1) = (byte) buttonAction.Button;
						*(ptr2 + length + 2) = (byte) buttonAction.Action;
						length += 3;
						break;
					case AxisAction axisAction:
						*(ptr2 + length) = (int) InputActionType.Axis;
						*(ptr2 + length + 1) = (byte) axisAction.Axis;
						*(float*) (ptr2 + length + 2) = axisAction.Value;
						length += 6;
						break;
					case ZoomAction zoomAction:
						*(ptr2 + length) = (int) InputActionType.Zoom;
						*(float*) (ptr2 + length + 1) = zoomAction.Value;
						length += 5;
						break;
					case CallbackAction callbackAction:
						*(ptr2 + length) = (int) InputActionType.Callback;
#if NET6_0_OR_GREATER
						callbackAction.Token.TryWriteBytes(buffer.AsSpan(length + 6, 16));
#else
						Array.Copy(callbackAction.Token.ToByteArray(), 0, buffer, length + 6, 16);
#endif
						length += 17;
						break;
					default:
						throw new ArgumentException("Invalid action type.");
				}
			}
		}
		return length;
	}
}

/// <summary>The notification that a <see cref="CallbackAction"/> has been reached.</summary>
public struct InputCallbackMessage(Guid token) : IDefuserMessage {
	/// <summary>The <see cref="Guid"/> provided in the <see cref="CallbackAction"/>.</summary>
	public Guid Token = token;

	readonly MessageType IDefuserMessage.MessageType => MessageType.InputCallback;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
#if NET6_0_OR_GREATER
		Token.TryWriteBytes(buffer.AsSpan(5));
#else
		Array.Copy(Token.ToByteArray(), 0, buffer, 5, 16);
#endif
		return 16;
	}
}

/// <summary>A command that immediately cancels queued controller inputs.</summary>
public struct CancelCommandMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.CancelCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}

/// <summary>A command that takes a screenshot of the game.</summary>
public struct ScreenshotCommandMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.ScreenshotCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}

/// <summary>A command that retrieves the name of the module in a specified component slot.</summary>
public struct CheatGetModuleTypeCommandMessage(Slot slot) : IDefuserMessage {
	public Slot Slot = slot;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) Slot.Bomb;
		buffer[6] = (byte) Slot.Face;
		buffer[7] = (byte) Slot.X;
		buffer[8] = (byte) Slot.Y;
		return 4;
	}
}

/// <summary>The response to a <see cref="CheatGetModuleTypeCommandMessage"/>.</summary>
public struct CheatGetModuleTypeResponseMessage(string? moduleType) : IDefuserMessage {
	public string? ModuleType = moduleType;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeResponse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => ModuleType is not null ? Encoding.UTF8.GetBytes(ModuleType, 0, ModuleType.Length, buffer, 5) : 0;
}

/// <summary>The response to a <see cref="CheatGetModuleTypeCommandMessage"/> if an error occurs.</summary>
public struct CheatGetModuleTypeErrorMessage(string message) : IDefuserMessage {
	public string Message = message ?? throw new ArgumentNullException(nameof(message));

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeError;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(Message, 0, Message.Length, buffer, 5);
}

/// <summary>A command that reads internal data from a module.</summary>
public struct CheatReadCommandMessage(Slot slot, IList<string> members) : IDefuserMessage {
	public Slot Slot = slot;
	public IList<string> Members = members ?? throw new ArgumentNullException(nameof(members));

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) Slot.Bomb;
		buffer[6] = (byte) Slot.Face;
		buffer[7] = (byte) Slot.X;
		buffer[8] = (byte) Slot.Y;
		var n = 9;
		foreach (var member in Members) {
			buffer[n] = (byte) member.Length;
			n++;
			n += Encoding.UTF8.GetBytes(member, 0, member.Length, buffer, n);
		}
		return n - 5;
	}
}

/// <summary>An event message that is sent when a game is starting.</summary>
public struct GameStartMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.GameStart;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}
/// <summary>An event message that is sent when a game is ending.</summary>
public struct GameEndMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.GameEnd;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}
/// <summary>An event message that is sent when a new bomb has been created.</summary>
public struct NewBombMessage(int numStrikes, int numSolvableModules, int numNeedyModules, TimeSpan time) : IDefuserMessage {
	public int NumStrikes = numStrikes;
	public int NumSolvableModules = numSolvableModules;
	public int NumNeedyModules = numNeedyModules;
	public TimeSpan Time = time;

	readonly MessageType IDefuserMessage.MessageType => MessageType.NewBomb;
	readonly unsafe int IDefuserMessage.ToBuffer(byte[] buffer) {
		fixed (byte* ptr = buffer) {
			buffer[5] = (byte) NumStrikes;
			*(short*) (ptr + 6) = (short) NumSolvableModules;
			*(short*) (ptr + 8) = (short) NumNeedyModules;
			*(long*) (ptr + 10) = Time.Ticks;
		}
		return 13;
	}
}
/// <summary>An event message that is sent when a strike has been issued.</summary>
public struct StrikeMessage(Slot slot) : IDefuserMessage {
	public Slot Slot = slot;

	readonly MessageType IDefuserMessage.MessageType => MessageType.Strike;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) Slot.Bomb;
		buffer[6] = (byte) Slot.Face;
		buffer[7] = (byte) Slot.X;
		buffer[8] = (byte) Slot.Y;
		return 4;
	}
}
/// <summary>An event message that is sent when the state of the alarm clock has changed.</summary>
public struct AlarmClockChangeMessage(bool on) : IDefuserMessage {
	public bool On = on;

	readonly MessageType IDefuserMessage.MessageType => MessageType.AlarmClockChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = On ? (byte) 1 : (byte) 0; return 1; }
}
/// <summary>An event message that is sent when the state of the lights has changed.</summary>
public struct LightsStateChangeMessage(bool on) : IDefuserMessage {
	public bool On = on;

	readonly MessageType IDefuserMessage.MessageType => MessageType.LightsStateChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = On ? (byte) 1 : (byte) 0; return 1; }
}
/// <summary>An event message that is sent when the state of a needy module has changed.</summary>
public struct NeedyStateChangeMessage(Slot slot, NeedyState state) : IDefuserMessage {
	public Slot Slot = slot;
	public NeedyState State = state;

	readonly MessageType IDefuserMessage.MessageType => MessageType.NeedyStateChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) Slot.Bomb;
		buffer[6] = (byte) Slot.Face;
		buffer[7] = (byte) Slot.X;
		buffer[8] = (byte) Slot.Y;
		buffer[9] = (byte) State;
		return 5;
	}
}
/// <summary>An event message that is sent when a bomb has exploded.</summary>
public struct BombExplodeMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.BombExplode;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}
/// <summary>An event message that is sent when a bomb has been defused.</summary>
public struct BombDefuseMessage(int bomb) : IDefuserMessage {
	public int Bomb = bomb;

	readonly MessageType IDefuserMessage.MessageType => MessageType.BombDefuse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = (byte) Bomb; return 1; }
}
