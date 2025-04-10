using System;
using System.IO;

namespace KtaneDefuserConnectorApi;
/// <summary>Writes defuser interface messages to a stream.</summary>
public class DefuserMessageWriter(Stream baseStream, byte[] buffer) : IDisposable {
	public Stream BaseStream { get; } = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

	public void Dispose() {
		BaseStream.Dispose();
		GC.SuppressFinalize(this);
	}

	~DefuserMessageWriter() => Dispose();

	public unsafe void Write(IDefuserMessage message) {
		lock (buffer) {
			var messageType = message.MessageType;
			fixed (byte* ptr = buffer) {
				var length = message.ToBuffer(buffer);
				buffer[0] = (byte) messageType;
				*(int*) (ptr + 1) = length;
				BaseStream.Write(buffer, 0, length + 5);
			}
		}
	}
}
