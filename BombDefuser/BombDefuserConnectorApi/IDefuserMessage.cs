using System;
using System.Collections.Generic;
using System.Text;

namespace BombDefuserConnectorApi;
/// <summary>A base interface for defuser interface messages.</summary>
public interface IDefuserMessage {
	MessageType MessageType { get; }
	int ToBuffer(byte[] buffer);
}

[Obsolete($"This message type is being replaced by specific Command messages.")]
public struct LegacyCommandMessage : IDefuserMessage {
	public string Command;

	public LegacyCommandMessage(string command) => this.Command = command ?? throw new ArgumentNullException(nameof(command));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(this.Command, 0, this.Command.Length, buffer, 5);
}

[Obsolete($"This message type is being replaced by specific event messages.")]
public struct LegacyEventMessage : IDefuserMessage {
	public string Event;

	public LegacyEventMessage(string eventMessage) => this.Event = eventMessage ?? throw new ArgumentNullException(nameof(eventMessage));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyEvent;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(this.Event, 0, this.Event.Length, buffer, 5);
}

/// <summary>The response to a <see cref="ScreenshotCommandMessage"/>.</summary>
public struct ScreenshotResponseMessage : IDefuserMessage {
	/// <summary>The length of the pixel data in bytes.</summary>
	public int PixelDataLength;
	/// <summary>The buffer containing the image width, height and pixel data.</summary>
	public byte[] Data;
	/// <summary>Returns the pixel width of the image.</summary>
	public readonly unsafe int Width {
		get {
			fixed (byte* ptr = this.Data)
				return *(int*) ptr;
		}
	}
	/// <summary>Returns the pixel height of the image.</summary>
	public readonly unsafe int Height {
		get {
			fixed (byte* ptr = this.Data)
				return *(int*) (ptr + 4);
		}
	}

	public ScreenshotResponseMessage(int pixelDataLength, byte[] data) {
		this.PixelDataLength = pixelDataLength;
		this.Data = data ?? throw new ArgumentNullException(nameof(data));
	}

	readonly MessageType IDefuserMessage.MessageType => MessageType.ScreenshotResponse;
	public readonly unsafe int ToBuffer(byte[] buffer) => this.PixelDataLength + 8;  // The caller should handle encoding the image data.
}

/// <summary>The response to a <see cref="CheatReadCommandMessage"/>.</summary>
public struct CheatReadResponseMessage : IDefuserMessage {
	/// <summary>A string representation of the data read.</summary>
	public string? Data;

	public CheatReadResponseMessage(string? data) => this.Data = data;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadResponse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => this.Data is not null ? Encoding.UTF8.GetBytes(this.Data, 0, this.Data.Length, buffer, 5) : 0;
}

/// <summary>The response to a <see cref="CheatReadCommandMessage"/> if an error occurs.</summary>
public struct CheatReadErrorMessage : IDefuserMessage {
	public string Message;

	public CheatReadErrorMessage(string message) => this.Message = message ?? throw new ArgumentNullException(nameof(message));

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadError;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(this.Message, 0, this.Message.Length, buffer, 5);
}

[Obsolete($"This message is being replaced with {nameof(InputCallbackMessage)}")]
public struct LegacyInputCallbackMessage : IDefuserMessage {
	public string Token;

	public LegacyInputCallbackMessage(string token) => this.Token = token ?? throw new ArgumentNullException(nameof(token));

	readonly MessageType IDefuserMessage.MessageType => MessageType.LegacyInputCallback;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(this.Token, 0, this.Token.Length, buffer, 5);
}

/// <summary>A command that sends controller inputs to the game.</summary>
public struct InputCommandMessage : IDefuserMessage {
	public IEnumerable<IInputAction> Actions;

	public InputCommandMessage(IEnumerable<IInputAction> actions) => this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));

	readonly MessageType IDefuserMessage.MessageType => MessageType.InputCommand;
	public readonly unsafe int ToBuffer(byte[] buffer) {
		var length = 0;
		fixed (byte* ptr = buffer) {
			foreach (var action in this.Actions) {
				var ptr2 = ptr + 5;
				switch (action) {
					case NoOpAction:
						*(ptr2 + length) = 0;
						length++;
						break;
					case ButtonAction buttonAction:
						*(ptr2 + length) = 1;
						*(ptr2 + length + 1) = (byte) buttonAction.Button;
						*(ptr2 + length + 2) = (byte) buttonAction.Action;
						length += 3;
						break;
					case AxisAction axisAction:
						*(ptr2 + length) = 2;
						*(ptr2 + length + 1) = (byte) axisAction.Axis;
						*(float*) (ptr2 + length + 2) = axisAction.Value;
						length += 6;
						break;
					case ZoomAction zoomAction:
						*(ptr2 + length) = 3;
						*(float*) (ptr2 + length + 1) = zoomAction.Value;
						length += 5;
						break;
					case CallbackAction callbackAction:
						*(ptr2 + length) = 4;
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
public struct InputCallbackMessage : IDefuserMessage {
	/// <summary>The <see cref="Guid"/> provided in the <see cref="CallbackAction"/>.</summary>
	public Guid Token;

	public InputCallbackMessage(Guid token) => this.Token = token;

	readonly MessageType IDefuserMessage.MessageType => MessageType.InputCallback;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
#if NET6_0_OR_GREATER
		this.Token.TryWriteBytes(buffer.AsSpan(5));
#else
		Array.Copy(this.Token.ToByteArray(), 0, buffer, 5, 16);
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
public struct CheatGetModuleTypeCommandMessage : IDefuserMessage {
	public Slot Slot;

	public CheatGetModuleTypeCommandMessage(Slot slot) => this.Slot = slot;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) this.Slot.Bomb;
		buffer[6] = (byte) this.Slot.Face;
		buffer[7] = (byte) this.Slot.X;
		buffer[8] = (byte) this.Slot.Y;
		return 4;
	}
}

/// <summary>The response to a <see cref="CheatGetModuleTypeCommandMessage"/>.</summary>
public struct CheatGetModuleTypeResponseMessage : IDefuserMessage {
	public string? ModuleType;

	public CheatGetModuleTypeResponseMessage(string? moduleType) => this.ModuleType = moduleType;

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeResponse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => this.ModuleType is not null ? Encoding.UTF8.GetBytes(this.ModuleType, 0, this.ModuleType.Length, buffer, 5) : 0;
}

/// <summary>The response to a <see cref="CheatGetModuleTypeCommandMessage"/> if an error occurs.</summary>
public struct CheatGetModuleTypeErrorMessage : IDefuserMessage {
	public string Message;

	public CheatGetModuleTypeErrorMessage(string message) => this.Message = message ?? throw new ArgumentNullException(nameof(message));

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatGetModuleTypeError;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => Encoding.UTF8.GetBytes(this.Message, 0, this.Message.Length, buffer, 5);
}

/// <summary>A command that reads internal data from a module.</summary>
public struct CheatReadCommandMessage : IDefuserMessage {
	public Slot Slot;
	public IList<string> Members;

	public CheatReadCommandMessage(Slot slot, IList<string> members) {
		this.Slot = slot;
		this.Members = members ?? throw new ArgumentNullException(nameof(members));
	}

	readonly MessageType IDefuserMessage.MessageType => MessageType.CheatReadCommand;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) this.Slot.Bomb;
		buffer[6] = (byte) this.Slot.Face;
		buffer[7] = (byte) this.Slot.X;
		buffer[8] = (byte) this.Slot.Y;
		var n = 9;
		foreach (var member in this.Members) {
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
public struct NewBombMessage : IDefuserMessage {
	public int NumStrikes;
	public int NumSolvableModules;
	public int NumNeedyModules;
	public TimeSpan Time;

	public NewBombMessage(int numStrikes, int numSolvableModules, int numNeedyModules, TimeSpan time) {
		this.NumStrikes = numStrikes;
		this.NumSolvableModules = numSolvableModules;
		this.NumNeedyModules = numNeedyModules;
		this.Time = time;
	}

	readonly MessageType IDefuserMessage.MessageType => MessageType.NewBomb;
	readonly unsafe int IDefuserMessage.ToBuffer(byte[] buffer) {
		fixed (byte* ptr = buffer) {
			buffer[5] = (byte) this.NumStrikes;
			*(short*) (ptr + 6) = (short) this.NumSolvableModules;
			*(short*) (ptr + 8) = (short) this.NumNeedyModules;
			*(long*) (ptr + 10) = this.Time.Ticks;
		}
		return 13;
	}
}
/// <summary>An event message that is sent when a strike has been issued.</summary>
public struct StrikeMessage : IDefuserMessage {
	public Slot Slot;

	public StrikeMessage(Slot slot) => this.Slot = slot;

	readonly MessageType IDefuserMessage.MessageType => MessageType.Strike;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) this.Slot.Bomb;
		buffer[6] = (byte) this.Slot.Face;
		buffer[7] = (byte) this.Slot.X;
		buffer[8] = (byte) this.Slot.Y;
		return 4;
	}
}
/// <summary>An event message that is sent when the state of the alarm clock has changed.</summary>
public struct AlarmClockChangeMessage : IDefuserMessage {
	public bool On;

	public AlarmClockChangeMessage(bool on) => this.On = on;

	readonly MessageType IDefuserMessage.MessageType => MessageType.AlarmClockChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = this.On ? (byte) 1 : (byte) 0; return 1; }
}
/// <summary>An event message that is sent when the state of the lights has changed.</summary>
public struct LightsStateChangeMessage : IDefuserMessage {
	public bool On;

	public LightsStateChangeMessage(bool on) => this.On = on;

	readonly MessageType IDefuserMessage.MessageType => MessageType.LightsStateChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = this.On ? (byte) 1 : (byte) 0; return 1; }
}
/// <summary>An event message that is sent when the state of a needy module has changed.</summary>
public struct NeedyStateChangeMessage : IDefuserMessage {
	public Slot Slot;
	public NeedyState State;

	public NeedyStateChangeMessage(Slot slot, NeedyState state) {
		this.Slot = slot;
		this.State = state;
	}

	readonly MessageType IDefuserMessage.MessageType => MessageType.NeedyStateChange;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) {
		buffer[5] = (byte) this.Slot.Bomb;
		buffer[6] = (byte) this.Slot.Face;
		buffer[7] = (byte) this.Slot.X;
		buffer[8] = (byte) this.Slot.Y;
		buffer[9] = (byte) this.State;
		return 5;
	}
}
/// <summary>An event message that is sent when a bomb has exploded.</summary>
public struct BombExplodeMessage : IDefuserMessage {
	readonly MessageType IDefuserMessage.MessageType => MessageType.BombExplode;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) => 0;
}
/// <summary>An event message that is sent when a bomb has been defused.</summary>
public struct BombDefuseMessage : IDefuserMessage {
	public int Bomb;

	public BombDefuseMessage(int bomb) => this.Bomb = bomb;

	readonly MessageType IDefuserMessage.MessageType => MessageType.BombDefuse;
	readonly int IDefuserMessage.ToBuffer(byte[] buffer) { buffer[5] = (byte) this.Bomb; return 1; }
}
