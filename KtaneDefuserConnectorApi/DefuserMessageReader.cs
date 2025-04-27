using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KtaneDefuserConnectorApi;
/// <summary>Reads defuser interface messages from a stream.</summary>
public class DefuserMessageReader : IDisposable {
	public Stream BaseStream { get; }
	private readonly byte[] buffer;

	public event EventHandler<DefuserMessageEventArgs>? MessageReceived;
	public event EventHandler<DisconnectedEventArgs>? Disconnected;

	public DefuserMessageReader(Stream baseStream, int bufferSize) {
		BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
		buffer = new byte[bufferSize];
#if NET6_0_OR_GREATER
		System.Threading.Tasks.Task.Run(() => ReadLoopAsync(baseStream));
#else
		baseStream.BeginRead(buffer, 0, 5, BaseStream_Read, null);
#endif
	}

	public void Dispose() {
		BaseStream.Dispose();
		GC.SuppressFinalize(this);
	}

	~DefuserMessageReader() => Dispose();

#if NET6_0_OR_GREATER
	private async System.Threading.Tasks.Task ReadLoopAsync(Stream stream) {
		try {
			while (true) {
				await stream.ReadExactlyAsync(buffer, 0, 5);
				var messageType = (MessageType) buffer[0];
				var length = BitConverter.ToInt32(buffer, 1);
				if (length == 0)
					ProcessMessage(messageType, 0);
				else {
					await stream.ReadExactlyAsync(buffer, 0, length);
					ProcessMessage(messageType, length);
				}
			}
		} catch (IOException ex) {
			Disconnected?.Invoke(this, new(ex));
			await stream.DisposeAsync();
		} catch (ObjectDisposedException) { }
	}
#else
	private void BaseStream_Read(IAsyncResult ar) {
		try {
			var n = BaseStream.EndRead(ar);
			if (n > 0) {
				var messageType = (MessageType) buffer[0];
				var length = BitConverter.ToInt32(buffer, 1);
				if (length == 0) {
					ProcessMessage(messageType, 0);
					BaseStream.BeginRead(buffer, 0, 5, BaseStream_Read, null);
				} else if (length < buffer.Length) {
					// Longer messages from the client are invalid.
					BaseStream.BeginRead(buffer, 0, length, BaseStream_Read2, messageType);
				}
			} else
				Disconnected?.Invoke(this, new(null));
		} catch (IOException ex) {
			Disconnected?.Invoke(this, new(ex));
			BaseStream.Dispose();
		}
	}

	private void BaseStream_Read2(IAsyncResult ar) {
		try {
			var n = BaseStream.EndRead(ar);
			if (n > 0) {
				ProcessMessage((MessageType) ar.AsyncState, n);
				BaseStream.BeginRead(buffer, 0, 5, BaseStream_Read, null);
			} else
				Disconnected?.Invoke(this, new(null));
		} catch (IOException ex) {
			Disconnected?.Invoke(this, new(ex));
			BaseStream.Dispose();
		}
	}
#endif

#pragma warning disable CS0618 // TODO: Obsolete message types may be removed later.
	private void ProcessMessage(MessageType messageType, int length) {
		lock (buffer) {
			IDefuserMessage message = messageType switch {
				MessageType.LegacyCommand => new LegacyCommandMessage(Encoding.UTF8.GetString(buffer, 0, length)),
				MessageType.LegacyEvent => new LegacyEventMessage(Encoding.UTF8.GetString(buffer, 0, length)),
				MessageType.ScreenshotResponse => new ScreenshotResponseMessage(length, buffer),
				MessageType.CheatReadResponse => new CheatReadResponseMessage(length > 0 ? Encoding.UTF8.GetString(buffer, 0, length) : null),
				MessageType.CheatReadError => new CheatReadErrorMessage(Encoding.UTF8.GetString(buffer, 0, length)),
				MessageType.LegacyInputCallback => new LegacyInputCallbackMessage(Encoding.UTF8.GetString(buffer, 0, length)),
				MessageType.InputCommand => ReadInputCommand(buffer, length),
				MessageType.InputCallback => ReadInputCallback(buffer),
				MessageType.CancelCommand => new CancelCommandMessage(),
				MessageType.ScreenshotCommand => new ScreenshotCommandMessage(),
				MessageType.CheatGetModuleTypeCommand => new CheatGetModuleTypeCommandMessage(new(buffer[0], buffer[1], buffer[2], buffer[3])),
				MessageType.CheatGetModuleTypeResponse => new CheatGetModuleTypeResponseMessage(length > 0 ? Encoding.UTF8.GetString(buffer, 0, length) : null),
				MessageType.CheatGetModuleTypeError => new CheatGetModuleTypeErrorMessage(Encoding.UTF8.GetString(buffer, 0, length)),
				MessageType.CheatReadCommand => ReadCheatReadCommand(buffer, length),
				MessageType.GameStart => new GameStartMessage(),
				MessageType.GameEnd => new GameEndMessage(),
				MessageType.NewBomb => ReadNewBomb(buffer),
				MessageType.Strike => new StrikeMessage(new(buffer[0], buffer[1], buffer[2], buffer[3])),
				MessageType.AlarmClockChange => new AlarmClockChangeMessage(buffer[0] != 0),
				MessageType.LightsStateChange => new LightsStateChangeMessage(buffer[0] != 0),
				MessageType.NeedyStateChange => new NeedyStateChangeMessage(new(buffer[0], buffer[1], buffer[2], buffer[3]), (NeedyState) buffer[4]),
				MessageType.BombExplode => new BombExplodeMessage(),
				MessageType.BombDefuse => new BombDefuseMessage(buffer[0]),
				_ => throw new InvalidOperationException("Unknown message type")
			};
			MessageReceived?.Invoke(this, new(message));
		}
	}
#pragma warning restore CS0618 // Type or member is obsolete

	private static unsafe InputCommandMessage ReadInputCommand(byte[] buffer, int length) {
		var actions = new List<IInputAction>();
		var n = 0;
		fixed (byte* ptr = buffer) {
			while (n < length) {
				var typeId = *(ptr + n);
				switch (typeId) {
					case (int) InputActionType.None:
						actions.Add(new NoOpAction());
						n++;
						break;
					case (int) InputActionType.Button:
						actions.Add(new ButtonAction((Button) (*(ptr + n + 1)), (ButtonActionType) (*(ptr + n + 2))));
						n += 3;
						break;
					case (int) InputActionType.Axis:
						actions.Add(new AxisAction((Axis) (*(ptr + n + 1)), *(float*) (ptr + n + 2)));
						n += 6;
						break;
					case (int) InputActionType.Zoom:
						actions.Add(new ZoomAction(*(float*) (ptr + n + 1)));
						n += 6;
						break;
					case (int) InputActionType.Callback:
						actions.Add(new CallbackAction(new(*(int*) (ptr + n + 1), *(short*) (ptr + n + 5), *(short*) (ptr + n + 7), *(ptr + n + 9), *(ptr + n + 10), *(ptr + n + 11), *(ptr + n + 12), *(ptr + n + 13), *(ptr + n + 14), *(ptr + n + 15), *(ptr + n + 16))));
						n += 17;
						break;
					default:
						throw new ArgumentException("Invalid action type.");
				}
			}
		}
		return new(actions);
	}

	private static unsafe InputCallbackMessage ReadInputCallback(byte[] buffer) {
		fixed (byte* ptr = buffer)
			return new(new(*(int*) ptr, *(short*) (ptr + 4), *(short*) (ptr + 6), buffer[8], buffer[9], buffer[10], buffer[11], buffer[12], buffer[13], buffer[14], buffer[15]));
	}

	private static CheatReadCommandMessage ReadCheatReadCommand(byte[] buffer, int length) {
		var slot = new Slot(buffer[0], buffer[1], buffer[2], buffer[3]);
		var members = new List<string>();
		var n = 4;
		while (n < length) {
			var length2 = buffer[n++];
			members.Add(Encoding.UTF8.GetString(buffer, n, length2));
			n += length2;
		}
		return new(slot, members);
	}

	private static unsafe NewBombMessage ReadNewBomb(byte[] buffer) {
		fixed (byte* ptr = buffer)
			return new(buffer[0], *(short*) (ptr + 1), *(short*) (ptr + 3), TimeSpan.FromTicks(*(long*) (ptr + 5)));
	}
}

public class DefuserMessageEventArgs(IDefuserMessage message) : EventArgs {
	public IDefuserMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
}

public class DisconnectedEventArgs(Exception? exception) : EventArgs {
	public Exception? Exception { get; } = exception;
}
