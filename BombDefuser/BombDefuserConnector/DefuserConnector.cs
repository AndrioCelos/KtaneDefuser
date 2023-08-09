using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BombDefuserConnectorApi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
public class DefuserConnector : IDisposable {
	private static readonly Dictionary<Type, ComponentReader> componentReaders
		= (from t in typeof(ComponentReader).Assembly.GetTypes() where !t.IsAbstract && typeof(ComponentReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (ComponentReader) Activator.CreateInstance(t)!);
	private static readonly Dictionary<Type, WidgetReader> widgetReaders
		= (from t in typeof(WidgetReader).Assembly.GetTypes() where !t.IsAbstract && typeof(WidgetReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (WidgetReader) Activator.CreateInstance(t)!);

	private (TcpClient tcpClient, DefuserMessageReader reader, DefuserMessageWriter writer)? connection;

	private TaskCompletionSource<Image<Rgba32>>? screenshotTaskSource;
	private TaskCompletionSource<string?>? readTaskSource;
	private readonly BlockingCollection<string> aimlNotificationQueue = new();
	private Simulation? simulation;

	private static DefuserConnector? instance;
	public static DefuserConnector Instance => instance ?? throw new InvalidOperationException("Service not yet initialised");
	public static Components.Timer TimerReader { get; }

	public bool IsConnected { get; private set; }
	public bool CallbacksEnabled { get; private set; }

	private const int PORT = 8086;

	static DefuserConnector() => TimerReader = GetComponentReader<Components.Timer>();

	public DefuserConnector() {
		instance ??= this;
	}

	~DefuserConnector() => this.Dispose();

	public void Dispose() {
		this.connection?.tcpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	public void EnableCallbacks() {
		this.CallbacksEnabled = true;
		var thread = new Thread(this.RunCallbackThread) { IsBackground = true, Name = $"{nameof(DefuserConnector)} callback thread" };
		thread.Start();
	}

	private void RunCallbackThread() {
		while (true) {
			var s = this.aimlNotificationQueue.Take();
			AimlVoice.Program.sendInput(s);
		}
	}

	public async Task ConnectAsync(bool simulation) {
		if (this.IsConnected) throw new InvalidOperationException("Cannot connect while already connected.");
		if (simulation) {
			this.simulation = new();
		} else {
			var tcpClient = new TcpClient();
			await tcpClient.ConnectAsync(new IPEndPoint(IPAddress.Loopback, PORT));
			var stream = tcpClient.GetStream();
			var reader = new DefuserMessageReader(stream, 14745612);
			reader.Disconnected += this.Reader_Disconnected;
			reader.MessageReceived += this.Reader_MessageReceived;
			this.connection = (tcpClient, reader, new(stream, new byte[1024]));
		}
		this.IsConnected = true;
	}

	private void Reader_Disconnected(object? sender, DisconnectedEventArgs e) => this.SendAimlNotification($"OOB DefuserSocketError {e.Exception?.Message ?? "nil"}");
	private void Reader_MessageReceived(object? sender, DefuserMessageEventArgs e) {
		switch (e.Message) {
			case LegacyEventMessage legacyEventMessage:
				this.SendAimlNotification($"OOB DefuserSocketMessage {legacyEventMessage.Event}");
				break;
			case ScreenshotResponseMessage screenshotResponseMessage:
				if (this.screenshotTaskSource is null) break;
				var image = Image.LoadPixelData<Rgba32>(screenshotResponseMessage.Data.AsSpan(8, screenshotResponseMessage.PixelDataLength), screenshotResponseMessage.Width, screenshotResponseMessage.Height);
				image.Mutate(c => c.Flip(FlipMode.Vertical));
#if DEBUG
				SaveDebugImage(image, "Screenshot");
#endif
				this.screenshotTaskSource.SetResult(image);
				break;
			case CheatReadResponseMessage cheatReadResponseMessage:
				this.readTaskSource?.SetResult(cheatReadResponseMessage.Data);
				break;
			case CheatReadErrorMessage cheatReadErrorMessage:
				this.readTaskSource?.SetException(new Exception($"Read failed: {cheatReadErrorMessage.Message}"));
				break;
			case LegacyInputCallbackMessage legacyInputCallbackMessage:
				this.SendAimlNotification($"OOB DefuserCallback {legacyInputCallbackMessage.Token}");
				break;
			case InputCallbackMessage inputCallbackMessage:
				this.SendAimlNotification($"OOB DefuserCallback {inputCallbackMessage.Token:N}");
				break;
			case CheatGetModuleTypeResponseMessage cheatGetModuleTypeResponseMessage:
				this.readTaskSource?.SetResult(cheatGetModuleTypeResponseMessage.ModuleType);
				break;
			case CheatGetModuleTypeErrorMessage cheatGetModuleTypeErrorMessage:
				this.readTaskSource?.SetException(new Exception($"Read failed: {cheatGetModuleTypeErrorMessage.Message}"));
				break;
			case GameStartMessage gameStartMessage:
				this.SendAimlNotification($"OOB GameStart");
				break;
			case GameEndMessage gameEndMessage:
				this.SendAimlNotification($"OOB GameEnd");
				break;
			case NewBombMessage newBombMessage:
				this.SendAimlNotification($"OOB NewBomb {newBombMessage.NumStrikes} {newBombMessage.NumSolvableModules} {newBombMessage.NumNeedyModules} {Math.Floor(newBombMessage.Time.TotalSeconds)}");
				break;
			case StrikeMessage strikeMessage:
				this.SendAimlNotification($"OOB Strike {strikeMessage.Slot.Bomb} {strikeMessage.Slot.Face} {strikeMessage.Slot.X} {strikeMessage.Slot.Y}");
				break;
			case AlarmClockChangeMessage alarmClockChangeMessage:
				this.SendAimlNotification($"OOB AlarmClockChange {alarmClockChangeMessage.On}");
				break;
			case LightsStateChangeMessage lightsStateChangeMessage:
				this.SendAimlNotification($"OOB LightsChange {lightsStateChangeMessage.On}");
				break;
			case NeedyStateChangeMessage needyStateChangeMessage:
				this.SendAimlNotification($"OOB NeedyStateChange {needyStateChangeMessage.Slot.Bomb} {needyStateChangeMessage.Slot.Face} {needyStateChangeMessage.Slot.X} {needyStateChangeMessage.Slot.Y} {needyStateChangeMessage.State}");
				break;
			case BombExplodeMessage bombExplodeMessage:
				this.SendAimlNotification($"OOB BombExplode");
				break;
			case BombDefuseMessage bombDefuseMessage:
				this.SendAimlNotification($"OOB BombDefuse {bombDefuseMessage.Bomb}");
				break;
		}
	}

	private void SendAimlNotification(string message) {
		if (this.CallbacksEnabled)
			this.aimlNotificationQueue.Add(message);
	}

	private void SendMessage(IDefuserMessage message) {
		if (this.connection is null) throw new InvalidOperationException("Not connected.");
		this.connection.Value.writer.Write(message);
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
			this.SendMessage(new ScreenshotCommandMessage());
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
				this.SendMessage(new LegacyCommandMessage($"input {inputs}"));
			} catch (Exception ex) {
				AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
			}
		}
	}
	public void SendInputs(params IInputAction[] actions) => this.SendMessage(new InputCommandMessage(actions));
	public void SendInputs(IEnumerable<IInputAction> actions) => this.SendMessage(new InputCommandMessage(actions));

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
		var looksLikeANeedyModule = needyRating >= 0.8f;

		var ratings = new List<(ComponentReader reader, float rating)>();
		foreach (var reader in componentReaders.Values) {
			if (reader.UsesNeedyFrame == looksLikeANeedyModule)
				ratings.Add((reader, reader.IsModulePresent(bitmap)));
		}

		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].reader;
	}

	public ComponentReader? CheatGetComponentReader(Slot slot) {
		if (this.simulation is not null)
			return this.simulation.GetComponentReader(slot);

		this.readTaskSource = new();
		this.SendMessage(new CheatGetModuleTypeCommandMessage(slot));
		var name = this.readTaskSource.Task.Result;
		return !string.IsNullOrEmpty(name) && typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{name}", false, true) is Type t ? componentReaders[t] : null;
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

	public string? CheatRead(Slot slot, params string[] members) {
		if (this.simulation is not null)
			throw new NotImplementedException();

		this.readTaskSource = new();
		this.SendMessage(new CheatReadCommandMessage(slot, members));
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
}
