using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
public class DefuserConnector {
	private static readonly Dictionary<Type, ComponentReader> componentReaders
		= (from t in typeof(ComponentReader).Assembly.GetTypes() where !t.IsAbstract && typeof(ComponentReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (ComponentReader) Activator.CreateInstance(t)!);
	private static readonly Dictionary<Type, WidgetReader> widgetReaders
		= (from t in typeof(WidgetReader).Assembly.GetTypes() where !t.IsAbstract && typeof(WidgetReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (WidgetReader) Activator.CreateInstance(t)!);

	private TcpClient? tcpClient;
	private readonly byte[] writeBuffer = new byte[1024];
	private readonly byte[] readBuffer = new byte[14745612];

	private TaskCompletionSource<Image<Rgba32>>? screenshotTaskSource;
	private TaskCompletionSource<string>? readTaskSource;
	private Simulation? simulation;

	private static DefuserConnector? instance;
	public static DefuserConnector Instance => instance ?? throw new InvalidOperationException("Service not yet initialised");
	public static Components.Timer TimerReader { get; }

	public bool IsConnected { get; private set; }

	private const int PORT = 8086;

	static DefuserConnector() => TimerReader = GetComponentReader<Components.Timer>();

	public DefuserConnector() => instance ??= this;

	public async Task ConnectAsync(bool simulation) {
		if (this.IsConnected) throw new InvalidOperationException("Cannot connect while already connected.");
		if (simulation) {
			this.simulation = new();
		} else {
			this.tcpClient = new TcpClient();
			await this.tcpClient.ConnectAsync(new IPEndPoint(IPAddress.Loopback, PORT));
			_ = this.ReadLoopAsync();
		}
		this.IsConnected = true;
	}

	private async Task ReadLoopAsync() {
		if (this.tcpClient == null) return;
		var stream = this.tcpClient.GetStream();
		try {
			while (true) {
				await stream.ReadExactlyAsync(this.readBuffer, 0, 5);
				var messageType = (MessageType)this.readBuffer[0];
				var length = BitConverter.ToInt32(this.readBuffer, 1);
				switch (messageType) {
					case MessageType.Event:
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						AimlVoice.Program.sendInput($"OOB DefuserSocketMessage {Encoding.UTF8.GetString(this.readBuffer, 0, length)}");
						break;
					case MessageType.Image: {
						if (this.screenshotTaskSource is null) break;
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						var w = BitConverter.ToInt32(this.readBuffer, 0);
						var h = BitConverter.ToInt32(this.readBuffer, 4);
						var image = Image.LoadPixelData<Rgba32>(this.readBuffer.AsSpan()[8..length], w, h);
						image.Mutate(c => c.Flip(FlipMode.Vertical));
#if DEBUG
						SaveDebugImage(image, "Screenshot");
#endif
						this.screenshotTaskSource.SetResult(image);
						break;
					}
					case MessageType.ReadResponse:
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						this.readTaskSource?.SetResult(Encoding.UTF8.GetString(this.readBuffer, 0, length));
						break;
					case MessageType.ReadError:
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						this.readTaskSource?.SetException(new Exception($"Read failed: {Encoding.UTF8.GetString(this.readBuffer, 0, length)}"));
						break;
					case MessageType.InputCallback:
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						_ = Task.Run(() => AimlVoice.Program.sendInput($"OOB DefuserCallback {Encoding.UTF8.GetString(this.readBuffer, 0, length)}"));
						break;
				}
			}
		} catch (Exception ex) {
			AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
		}
	}

	internal void SendMessage(string message) {
		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		var length = Encoding.UTF8.GetBytes(message, 0, message.Length, this.writeBuffer, 5);
		this.writeBuffer[0] = 1;
		Array.Copy(BitConverter.GetBytes(length), 0, this.writeBuffer, 1, 4);
		this.tcpClient.GetStream().Write(this.writeBuffer, 0, length + 5);
	}

#if DEBUG
	internal static void SaveDebugImage(Image image, string category) {
		var file = Path.Combine(Path.GetTempPath(), "BombDefuserDebug", $"{category}.{DateTime.Now:yyyy-MM-dd.HH-mm-ss}.{Guid.NewGuid()}.bmp");
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		image.Save(file);
	}
#endif

	public Image<Rgba32> TakeScreenshot() => this.TakeScreenshotAsync().Result;
	public async Task<Image<Rgba32>> TakeScreenshotAsync() {
		if (this.simulation is not null)
			return Simulation.DummyScreenshot;
		if (this.screenshotTaskSource is not null) throw new InvalidOperationException("Already taking a screenshot.");
		try {
			this.screenshotTaskSource = new();
			this.SendMessage("screenshot");
			return await this.screenshotTaskSource.Task;
		} finally {
			this.screenshotTaskSource = null;
		}
	}

	public void SendInputs(string inputs) {
		if (this.simulation is not null)
			this.simulation.SendInputs(inputs);
		else {
			try {
				this.SendMessage($"input {inputs}");
			} catch (Exception ex) {
				AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
			}
		}
	}

	public void CheatSolve() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.Solve();
	}

	public void CheatStrike() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.Strike();
	}

	public void CheatTriggerAlarmClock() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.SetAlarmClock(true);
	}

	private static bool IsBombBacking(HsvColor hsv) => hsv.H is >= 180 and < 225 && hsv.S < 0.35f && hsv.V >= 0.35f;

	public int GetSideWidgetAdjustment(Image<Rgba32> screenshotBitmap) {
		if (this.simulation is not null)
			return 0;
		int left;
		for (left = 60; left < screenshotBitmap.Width - 60; left++) {
			if (IsBombBacking(HsvColor.FromColor(screenshotBitmap[left, screenshotBitmap.Height / 2])))
				break;
		}
		int right;
		for (right = screenshotBitmap.Width - 60; right >= 60; right--) {
			if (IsBombBacking(HsvColor.FromColor(screenshotBitmap[right, screenshotBitmap.Height / 2])))
				break;
		}
		return (left + right) / 2 - 988;
	}

	public static LightsState GetLightsState(Image<Rgba32> screenshot) => screenshot[screenshot.Width / 2, 0].R switch {
		< 17 => LightsState.Off,
		< 32 => LightsState.Buzz,
		< 75 => LightsState.On,
		_ => LightsState.Emergency
	};

	public ModuleLightState GetModuleLightState(Image<Rgba32> screenshotBitmap, IReadOnlyList<Point> points)
		=> this.simulation is not null
			? this.simulation.GetLightState(points[0])
			: ImageUtils.GetLightState(screenshotBitmap, points);

	public static T GetComponentReader<T>() where T : ComponentReader => (T) componentReaders[typeof(T)];

	public ComponentReader? GetComponentReader(Image<Rgba32> screenshotBitmap, IReadOnlyList<Point> points) {
		if (this.simulation is not null)
			return this.simulation.GetComponentReader(points[0]);

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
			points,
			InterpolationMode.NearestNeighbour);

		if (ImageUtils.CheckForBlankComponent(bitmap))
			return null;

		var needyRating = ImageUtils.CheckForNeedyFrame(bitmap);
		var looksLikeANeedyModule = needyRating >= 0.5f;

		var ratings = new List<(ComponentReader reader, float rating)>();
		foreach (var reader in componentReaders.Values) {
			if (reader.UsesNeedyFrame == looksLikeANeedyModule)
				ratings.Add((reader, reader.IsModulePresent(bitmap)));
		}

		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].reader;
	}

	public ComponentReader? CheatGetComponentReader(int face, int x, int y) {
		if (this.simulation is not null)
			return this.simulation.GetComponentReader(face, x, y);

		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		this.readTaskSource = new();
		this.SendMessage($"getmodulename {face} {x} {y}");
		var name = this.readTaskSource.Task.Result;
		return !string.IsNullOrEmpty(name) && typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{name}") is Type t ? componentReaders[t] : null;
	}

	public WidgetReader? GetWidgetReader(Image<Rgba32> screenshotBitmap, IReadOnlyList<Point> points) {
		if (this.simulation is not null)
			return this.simulation.GetWidgetReader(points[0]);

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
			points,
			InterpolationMode.NearestNeighbour);
		var pixelCounts = WidgetReader.GetPixelCounts(bitmap, 0);

		var ratings = new List<(WidgetReader reader, float rating)>();
		foreach (var reader in widgetReaders.Values) {
			ratings.Add((reader, reader.IsWidgetPresent(bitmap, 0, pixelCounts)));
		}
		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].rating >= 0.25f ? ratings[0].reader : null;
	}

	public T ReadComponent<T>(Image<Rgba32> screenshot, ComponentReader<T> reader, IReadOnlyList<Point> polygon) where T : notnull {
		if (this.simulation is not null)
			return this.simulation.ReadComponent<T>(polygon[0]);
		var image = ImageUtils.PerspectiveUndistort(screenshot, polygon, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, reader.Name);
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, ref debugImage);
	}

	public T ReadWidget<T>(Image<Rgba32> screenshot, WidgetReader<T> reader, IReadOnlyList<Point> polygon) where T : notnull {
		if (this.simulation is not null)
			return this.simulation.ReadWidget<T>(polygon[0]);
		var image = ImageUtils.PerspectiveUndistort(screenshot, polygon, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, reader.Name);
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, 0, ref debugImage);
	}

	public string CheatRead(string[] tokens) {
		if (this.simulation is not null)
			throw new NotImplementedException();

		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		this.readTaskSource = new();
		this.SendMessage($"read {string.Join(' ', tokens.Skip(1))}");
		return this.readTaskSource.Task.Result;
	}

	internal string? Read(string readerName, Image<Rgba32> screenshot, Point[] points) {
		if (this.simulation is not null)
			return typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{readerName}", false, true) is Type t
				? this.simulation.ReadModule(t.Name, points[0])
				: typeof(WidgetReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Widgets)}.{readerName}", false, true) is Type t2
				? this.simulation.ReadWidget(t2.Name, points[0])
				: throw new ArgumentException($"No such command, module or widget is known: {readerName}");

		var image = ImageUtils.PerspectiveUndistort(screenshot, points, InterpolationMode.NearestNeighbour);
		Image<Rgba32>? debugImage = null;
#if DEBUG
		SaveDebugImage(image, readerName);
#endif

		var type = typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{readerName}");
		if (type is not null)
			return componentReaders[type].ProcessNonGeneric(image, ref debugImage)?.ToString();

		type = typeof(WidgetReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Widgets)}.{readerName}");
		if (type is not null)
			return widgetReaders[type].ProcessNonGeneric(image, 0, ref debugImage)?.ToString();

		throw new ArgumentException($"No such command, component or widget is known: {readerName}");
	}

	public enum MessageType {
		Command,
		Event,
		Image,
		ReadResponse,
		ReadError,
		InputCallback
	}
}
