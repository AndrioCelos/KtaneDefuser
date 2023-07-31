using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aiml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
public class BombDefuserAimlService : ISraixService {
	private TcpClient? tcpClient;
	private readonly byte[] writeBuffer = new byte[1024];
	private readonly byte[] readBuffer = new byte[14745612];
	//private static readonly HttpClient httpClient = new();
	//private ClientWebSocket? webSocket;
	//private CancellationTokenSource? tokenSource;
	private static readonly List<ComponentProcessor> moduleProcessors
		= typeof(ComponentProcessor).Assembly.GetTypes().Where(typeof(ComponentProcessor).IsAssignableFrom).Where(t => !t.IsAbstract)
		.Select(t => (ComponentProcessor) Activator.CreateInstance(t)!).ToList();
	private static readonly List<WidgetProcessor> widgetProcessors
		= typeof(WidgetProcessor).Assembly.GetTypes().Where(typeof(WidgetProcessor).IsAssignableFrom).Where(t => !t.IsAbstract)
		.Select(t => (WidgetProcessor) Activator.CreateInstance(t)!).ToList();
	private static readonly Dictionary<string, Image<Rgb24>> cachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private static readonly Queue<string> cachedScreenshotKeys = new();
	private string screenshotToken = "nil";
	private TaskCompletionSource<string>? readTaskSource;
	private Simulation? simulation;

	private static BombDefuserAimlService? instance;
	public static BombDefuserAimlService Instance => instance ?? throw new InvalidOperationException("Service not yet initialised");

	public static Components.Timer TimerProcessor { get; }

	static BombDefuserAimlService() => TimerProcessor = moduleProcessors.OfType<Components.Timer>().First();

	public BombDefuserAimlService() {
		instance = this;
		AimlVoice.Program.OobHandlers["takescreenshot"] = e => {
			this.screenshotToken = e.Attributes["token"]?.Value ?? e.GetElementsByTagName("token").Cast<XmlNode>().FirstOrDefault()?.InnerText ?? "nil";
			if (this.simulation is not null) {
				Task.Run(async () => {
					await Task.Delay(50);
					var token = this.screenshotToken;
					this.screenshotToken = "nil";
					AimlVoice.Program.sendInput($"OOB ScreenshotReady {token} dummy");
				});
			}
			else
				this.SendMessage("screenshot");
		};
		AimlVoice.Program.OobHandlers["tasconnect"] = e => {
			if (e.HasAttribute("simulation")) {
				this.simulation = new();
				cachedScreenshots["dummy"] = new(1, 1);
				AimlVoice.Program.sendInput("OOB DefuserSocketConnected");
			} else {
				try {
					this.tcpClient = new TcpClient("localhost", 8086);
					_ = this.readLoopAsync();
					AimlVoice.Program.sendInput("OOB DefuserSocketConnected");
				} catch (Exception ex) {
					AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
				}
			}
		};
		AimlVoice.Program.OobHandlers["sendinputs"] = e => {
			if (this.simulation is not null)
				this.simulation.SendInputs(e.InnerText);
			else {
				try {
					var text = e.InnerText;
					this.SendMessage($"input {text}");
				} catch (Exception ex) {
					AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
				}
			}
		};
		AimlVoice.Program.OobHandlers["solve"] = e => {
			if (this.simulation is not null)
				this.simulation.Solve();
			else
				throw new InvalidOperationException("No simulation active");
		};
		AimlVoice.Program.OobHandlers["strike"] = e => {
			if (this.simulation is not null)
				this.simulation.Solve();
			else
				throw new InvalidOperationException("No simulation active");
		};
		AimlVoice.Program.OobHandlers["triggeralarmclock"] = e => {
			if (this.simulation is not null)
				this.simulation.SetAlarmClock(true);
			else
				throw new InvalidOperationException("No simulation active");
		};
	}

	private void SendMessage(string message) {
		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		var length = Encoding.UTF8.GetBytes(message, 0, message.Length, this.writeBuffer, 5);
		this.writeBuffer[0] = 1;
		Array.Copy(BitConverter.GetBytes(length), 0, this.writeBuffer, 1, 4);
		this.tcpClient.GetStream().Write(this.writeBuffer, 0, length + 5);
	}

	private async Task readLoopAsync() {
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
						await stream.ReadExactlyAsync(this.readBuffer, 0, length);
						var w = BitConverter.ToInt32(this.readBuffer, 0);
						var h = BitConverter.ToInt32(this.readBuffer, 4);
						using var image0 = Image.LoadPixelData<Rgba32>(this.readBuffer.AsSpan()[8..length], w, h);
						var image = image0.CloneAs<Rgb24>();
						image.Mutate(c => c.Flip(FlipMode.Vertical));
						var newKey = Guid.NewGuid().ToString("N");
						lock (cachedScreenshots) {
							if (cachedScreenshotKeys.Count >= 100) {
								var toRemove = cachedScreenshotKeys.Dequeue();
								cachedScreenshots[toRemove].Dispose();
								cachedScreenshots.Remove(toRemove);
							}
							cachedScreenshotKeys.Enqueue(newKey);
							cachedScreenshots[newKey] = image;
							Console.WriteLine($"Taken screenshot as {newKey}");
						}
#if DEBUG
						SaveDebugImage(image, "Screenshot");
#endif
						var token = this.screenshotToken;
						this.screenshotToken = "nil";
						_ = Task.Run(() => AimlVoice.Program.sendInput($"OOB ScreenshotReady {token} {newKey}"));  // This could set a new token.
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

#if DEBUG
	internal static void SaveDebugImage(Image<Rgb24> image, string category) {
		var file = Path.Combine(Path.GetTempPath(), "BombDefuserDebug", $"{category}.{DateTime.Now:yyyy-MM-dd.HH-mm-ss}.{Guid.NewGuid()}.bmp");
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		image.Save(file);
	}
#endif

	private static bool isBombBacking(HsvColor hsv) => hsv.H is >= 180 and < 225 && hsv.S < 0.35f && hsv.V >= 0.35f;
	
	public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) {
		var tokens = text.Split();
		if (tokens[0].Equals("GetSideWidgetAdjustment", StringComparison.InvariantCultureIgnoreCase)) {
			var screenshotBitmap = cachedScreenshots[tokens[1]];
			return this.GetSideWidgetAdjustment(screenshotBitmap).ToString();
		} else if (tokens[0].Equals("IdentifyModule", StringComparison.InvariantCultureIgnoreCase)) {
			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var points = new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) };
			return this.GetComponentProcessor(screenshotBitmap, points)?.GetType().Name ?? "nil";
		} else if (tokens[0].Equals("IdentifyWidget", StringComparison.InvariantCultureIgnoreCase)) {
			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var points = new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) };
			return this.GetWidgetProcessor(screenshotBitmap, points)?.GetType().Name ?? "nil";
		} else if (tokens[0].Equals("GetLightState", StringComparison.InvariantCultureIgnoreCase)) {
			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var points = new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) };
			return this.GetLightState(screenshotBitmap, points).ToString();
		} else if (tokens[0].Equals("Read", StringComparison.InvariantCultureIgnoreCase)) {
			return this.CheatRead(tokens);
		} else if (tokens[0].Equals("GetModuleName", StringComparison.InvariantCultureIgnoreCase)) {
			return this.CheatGetComponentProcessor(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]))?.GetType().Name ?? "nil";
		} else {
			if (this.simulation is not null) {
				return moduleProcessors.Any(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase))
					? this.simulation.ReadModule(tokens[0], int.Parse(tokens[2]), int.Parse(tokens[3]))
					: widgetProcessors.Any(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase))
					? this.simulation.ReadWidget(tokens[0], int.Parse(tokens[2]), int.Parse(tokens[3]))
					: throw new ArgumentException($"No such command, module or widget is known: {tokens[0]}");
			}

			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var image = ImageUtils.PerspectiveUndistort(screenshotBitmap,
				new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) },
				InterpolationMode.NearestNeighbour);

#if DEBUG
			SaveDebugImage(image, tokens[0]);
#endif

			var debugBitmap = image.Clone();

			var processor = moduleProcessors.FirstOrDefault(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase));
			if (processor is not null) {
				var result = processor.ProcessNonGeneric(image, ref debugBitmap);
				return result?.ToString() ?? "nil";
			}

			var processor2 = widgetProcessors.FirstOrDefault(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase));
			if (processor2 is not null) {
				var result = processor2.ProcessNonGeneric(image, 0, ref debugBitmap);
				return result?.ToString() ?? "nil";
			}

			throw new ArgumentException($"No such command, module or widget is known: {tokens[0]}");
		}
	}

	public Image<Rgb24> GetScreenshot(string screenshotID) => cachedScreenshots[screenshotID];

	public int GetSideWidgetAdjustment(string screenshotID) => this.GetSideWidgetAdjustment(cachedScreenshots[screenshotID]);
	public int GetSideWidgetAdjustment(Image<Rgb24> screenshotBitmap) {
		if (this.simulation is not null)
			return 0;
		int left;
		for (left = 60; left < screenshotBitmap.Width - 60; left++) {
			if (isBombBacking(HsvColor.FromColor(screenshotBitmap[left, screenshotBitmap.Height / 2])))
				break;
		}
		int right;
		for (right = screenshotBitmap.Width - 60; right >= 60; right--) {
			if (isBombBacking(HsvColor.FromColor(screenshotBitmap[right, screenshotBitmap.Height / 2])))
				break;
		}
		return (left + right) / 2 - 988;
	}

	public ComponentProcessor? GetComponentProcessor(string screenshotID, IReadOnlyList<Point> points) => this.GetComponentProcessor(cachedScreenshots[screenshotID], points);
	public ComponentProcessor? GetComponentProcessor(Image<Rgb24> screenshotBitmap, IReadOnlyList<Point> points) {
		if (this.simulation is not null)
			return this.simulation.IdentifyComponent(points[0].X, points[0].Y) is string s ? moduleProcessors.First(p => p.GetType().Name == s) : null;

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
			points,
			InterpolationMode.NearestNeighbour);

		if (ImageUtils.CheckForBlankComponent(bitmap))
			return null;

		var needyRating = ImageUtils.CheckForNeedyFrame(bitmap);
		var looksLikeANeedyModule = needyRating >= 0.5f;

		var ratings = new List<(ComponentProcessor processor, float rating)>();
		foreach (var processor in moduleProcessors) {
			if (processor.UsesNeedyFrame == looksLikeANeedyModule)
				ratings.Add((processor, processor.IsModulePresent(bitmap)));
		}

		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].processor;
	}

	public WidgetProcessor? GetWidgetProcessor(string screenshotID, IReadOnlyList<Point> points) => this.GetWidgetProcessor(cachedScreenshots[screenshotID], points);
	public WidgetProcessor? GetWidgetProcessor(Image<Rgb24> screenshotBitmap, IReadOnlyList<Point> points) {
		if (this.simulation is not null)
			return this.simulation.IdentifyWidget(points[0].X, points[0].Y) is string s ? widgetProcessors.First(p => p.GetType().Name == s) : null;

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
			points,
			InterpolationMode.NearestNeighbour);
		var pixelCounts = WidgetProcessor.GetPixelCounts(bitmap, 0);

		var ratings = new List<(WidgetProcessor processor, float rating)>();
		foreach (var processor in widgetProcessors) {
			ratings.Add((processor, processor.IsWidgetPresent(bitmap, 0, pixelCounts)));
		}
		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].rating >= 0.25f ? ratings[0].processor : null;
	}

	public static T GetComponentProcessor<T>() where T : ComponentProcessor => moduleProcessors.OfType<T>().First();

	public T ReadComponent<T>(string screenshotID, ComponentProcessor<T> processor, IReadOnlyList<Point> polygon) where T : notnull => ReadComponent(cachedScreenshots[screenshotID], processor, polygon);
	public T ReadComponent<T>(Image<Rgb24> screenshot, ComponentProcessor<T> processor, IReadOnlyList<Point> polygon) where T : notnull {
		if (this.simulation is not null)
			return this.simulation.ReadComponent<T>(processor, polygon[0].X, polygon[0].Y);
		var image = ImageUtils.PerspectiveUndistort(screenshot, polygon, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, processor.Name);
#endif
		Image<Rgb24>? debugBitmap = null;
		return processor.Process(image, ref debugBitmap);
	}

	public T ReadWidget<T>(string screenshotID, WidgetProcessor<T> processor, IReadOnlyList<Point> polygon) where T : notnull => ReadWidget(cachedScreenshots[screenshotID], processor, polygon);
	public T ReadWidget<T>(Image<Rgb24> screenshot, WidgetProcessor<T> processor, IReadOnlyList<Point> polygon) where T : notnull {
			if (this.simulation is not null)
			return this.simulation.ReadWidget<T>(processor, polygon[0].X, polygon[0].Y);
		var image = ImageUtils.PerspectiveUndistort(screenshot, polygon, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, processor.Name);
#endif
		Image<Rgb24>? debugBitmap = null;
		return processor.Process(image, 0, ref debugBitmap);
	}

	public ModuleLightState GetLightState(string screenshotID, IReadOnlyList<Point> points) => this.GetLightState(cachedScreenshots[screenshotID], points);
	public ModuleLightState GetLightState(Image<Rgb24> screenshotBitmap, IReadOnlyList<Point> points)
		=> this.simulation is not null
			? this.simulation.GetLightState(points[0].X, points[0].Y)
			: ImageUtils.GetLightState(screenshotBitmap, points);

	public string CheatRead(string[] tokens) {
		if (this.simulation is not null)
			throw new NotImplementedException();

		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		this.readTaskSource = new();
		this.SendMessage($"read {string.Join(' ', tokens.Skip(1))}");
		return this.readTaskSource.Task.Result;
	}

	public ComponentProcessor? CheatGetComponentProcessor(int face, int x, int y) {
		if (this.simulation is not null)
			return this.simulation.IdentifyComponent(face, x, y) is string s ? moduleProcessors.First(p => p.GetType().Name == s) : null;

		if (this.tcpClient == null)
			throw new InvalidOperationException("Socket not connected");
		this.readTaskSource = new();
		this.SendMessage($"getmodulename {face} {x} {y}");
		var name = this.readTaskSource.Task.Result;
		return moduleProcessors.FirstOrDefault(p => p.GetType().Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
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
