using System;
using System.IO;

namespace BombDefuserConnectorApi;
public class DefuserMessageWriter : IDisposable {
	public Stream BaseStream { get; }
	private readonly byte[] buffer;

	public DefuserMessageWriter(Stream baseStream, byte[] buffer) {
		this.BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
		this.buffer = buffer;
	}

	public void Dispose() {
		this.BaseStream.Dispose();
		GC.SuppressFinalize(this);
	}

	~DefuserMessageWriter() => this.Dispose();

	public unsafe void Write(IDefuserMessage message) {
		lock (this.buffer) {
			var messageType = message.MessageType;
			fixed (byte* ptr = this.buffer) {
				var length = message.ToBuffer(this.buffer);
				this.buffer[0] = (byte) messageType;
				*(int*) (ptr + 1) = length;
				this.BaseStream.Write(this.buffer, 0, length + 5);
			}
		}
	}
}
