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
	private byte[] writeBuffer = new byte[1024];
	private byte[] readBuffer = new byte[14745612];
	//private static readonly HttpClient httpClient = new();
	//private ClientWebSocket? webSocket;
	//private CancellationTokenSource? tokenSource;
	private static readonly List<ModuleProcessor> moduleProcessors
		= typeof(ModuleProcessor).Assembly.GetTypes().Where(typeof(ModuleProcessor).IsAssignableFrom).Where(t => !t.IsAbstract)
		.Select(t => (ModuleProcessor) Activator.CreateInstance(t)!).ToList();
	private static readonly List<WidgetProcessor> widgetProcessors
		= typeof(WidgetProcessor).Assembly.GetTypes().Where(typeof(WidgetProcessor).IsAssignableFrom).Where(t => !t.IsAbstract)
		.Select(t => (WidgetProcessor) Activator.CreateInstance(t)!).ToList();
	private readonly Dictionary<string, Image<Rgb24>> cachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Queue<string> cachedScreenshotKeys = new();
	private string screenshotToken = "nil";
	private TaskCompletionSource<string>? readTaskSource;
	private Simulation? simulation;

	public BombDefuserAimlService() {
		AimlVoice.Program.OobHandlers["takescreenshot"] = e => {
			this.screenshotToken = e.Attributes["token"]?.Value ?? e.GetElementsByTagName("token").Cast<XmlNode>().FirstOrDefault()?.InnerText ?? "nil";
			if (simulation is not null)
				simulation.SimulateScreenshot(this.screenshotToken);
			else
				this.SendMessage("screenshot");
		};
		AimlVoice.Program.OobHandlers["tasconnect"] = e => {
			if (e.HasAttribute("simulation")) {
				simulation = new();
				AimlVoice.Program.sendInput("OOB DefuserSocketConnected");
			} else {
				try {
					tcpClient = new TcpClient("localhost", 8086);
					_ = readLoopAsync();
					AimlVoice.Program.sendInput("OOB DefuserSocketConnected");
				} catch (Exception ex) {
					AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
				}
			}
		};
		AimlVoice.Program.OobHandlers["sendinputs"] = e => {
			if (simulation is not null)
				simulation.SendInputs(e.InnerText);
			else {
				try {
					var text = e.InnerText;
					SendMessage($"input {text}");
				} catch (Exception ex) {
					AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
				}
			}
		};
		AimlVoice.Program.OobHandlers["solve"] = e => {
			if (simulation is not null)
				simulation.Solve();
			else
				throw new InvalidOperationException("No simulation active");
		};
		AimlVoice.Program.OobHandlers["strike"] = e => {
			if (simulation is not null)
				simulation.Solve();
			else
				throw new InvalidOperationException("No simulation active");
		};
		AimlVoice.Program.OobHandlers["triggeralarmclock"] = e => {
			if (simulation is not null)
				simulation.SetAlarmClock(true);
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
		if (tcpClient == null) return;
		var stream = tcpClient.GetStream();
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
						AimlVoice.Program.sendInput($"OOB ScreenshotReady {token} {newKey}");  // This could set a new token.
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
						AimlVoice.Program.sendInput($"OOB DefuserCallback {Encoding.UTF8.GetString(this.readBuffer, 0, length)}");
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
			if (simulation is not null)
				return "0";
			var screenshotBitmap = cachedScreenshots[tokens[1]];
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
			return ((left + right) / 2 - 988).ToString();
		} else if (tokens[0].Equals("IdentifyModule", StringComparison.InvariantCultureIgnoreCase)) {
			if (simulation is not null)
				return simulation.IdentifyModule(int.Parse(tokens[2]), int.Parse(tokens[3]));

			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
				new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) },
				InterpolationMode.NearestNeighbour);

			if (ImageUtils.CheckForBlankComponent(bitmap))
				return "nil";

			var needyRating = ImageUtils.CheckForNeedyFrame(bitmap);
			var looksLikeANeedyModule = needyRating >= 0.5f;

			var ratings = new List<(string moduleType, float rating)>();
			foreach (var processor in moduleProcessors) {
				if (processor.UsesNeedyFrame == looksLikeANeedyModule)
					ratings.Add((processor.GetType().Name, processor.IsModulePresent(bitmap)));
			}

			ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
			return ratings[0].moduleType;
		} else if (tokens[0].Equals("IdentifyWidget", StringComparison.InvariantCultureIgnoreCase)) {
			if (simulation is not null)
				return simulation.IdentifyWidget(int.Parse(tokens[2]), int.Parse(tokens[3]));

			var screenshotBitmap = cachedScreenshots[tokens[1]];
			var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap,
				new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) },
				InterpolationMode.NearestNeighbour);
			var pixelCounts = WidgetProcessor.GetPixelCounts(bitmap, 0);

			var ratings = new List<(string widgetType, float rating)>();
			foreach (var processor in widgetProcessors) {
				ratings.Add((processor.GetType().Name, processor.IsWidgetPresent(bitmap, 0, pixelCounts)));
			}
			ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
			return ratings[0].rating >= 0.25f ? ratings[0].widgetType : "nil";
		} else if (tokens[0].Equals("GetLightState", StringComparison.InvariantCultureIgnoreCase)) {
			if (simulation is not null)
				return simulation.GetLightState(int.Parse(tokens[2]), int.Parse(tokens[3]));

			var screenshotBitmap = cachedScreenshots[tokens[1]];
			return ImageUtils.GetLightState(screenshotBitmap, new Point[] { new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])) }).ToString();
		} else if (tokens[0].Equals("Read", StringComparison.InvariantCultureIgnoreCase)) {
			if (simulation is not null)
				throw new NotImplementedException();

			if (this.tcpClient == null)
				throw new InvalidOperationException("Socket not connected");
			this.readTaskSource = new();
			this.SendMessage($"read {string.Join(' ', tokens.Skip(1))}");
			return this.readTaskSource.Task.Result;
		} else {
			if (simulation is not null) {
				return moduleProcessors.Any(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase))
					? simulation.ReadModule(tokens[0], int.Parse(tokens[2]), int.Parse(tokens[3]))
					: widgetProcessors.Any(p => p.GetType().Name.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase))
					? simulation.ReadWidget(tokens[0], int.Parse(tokens[2]), int.Parse(tokens[3]))
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
				var result = processor2.Process(image, 0, ref debugBitmap);
				return result?.ToString() ?? "nil";
			}

			throw new ArgumentException($"No such command, module or widget is known: {tokens[0]}");
		}
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
