using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AngelAiml;
using JetBrains.Annotations;
using KtaneDefuserConnector.Widgets;
using KtaneDefuserConnectorApi;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Timer = KtaneDefuserConnector.Components.Timer;

namespace KtaneDefuserConnector;
/// <summary>Integrates a bot with Keep Talking and Nobody Explodes.</summary>
public class DefuserConnector : IDisposable {
	private const int Resolution = 305;
	
	private static readonly Dictionary<Type, ComponentReader> ComponentReaders
		= (from t in typeof(ComponentReader).Assembly.GetTypes() where !t.IsAbstract && typeof(ComponentReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (ComponentReader) Activator.CreateInstance(t)!);
	private static readonly Dictionary<string, ComponentReader> ComponentReadersByName;
	private static readonly MlComponentIdentifier ComponentIdentifier = new();
	private static DefuserConnector? _instance;
	public static DefuserConnector Instance => _instance ?? throw new InvalidOperationException("Service not yet initialised");

	/// <summary>Returns the <see cref="ComponentReader"/> instance representing the timer.</summary>
	public static Timer TimerReader { get; }
	public static BatteryHolder BatteryHolderReader { get; } = new();
	public static Indicator IndicatorReader { get; } = new();
	public static PortPlate PortPlateReader { get; } = new();
	public static SerialNumber SerialNumberReader { get; } = new();

	private (TcpClient tcpClient, DefuserMessageReader reader, DefuserMessageWriter writer)? _connection;

	private TaskCompletionSource<Image<Rgba32>>? _screenshotTaskSource;
	private TaskCompletionSource<string?>? _readTaskSource;
	private TaskCompletionSource<Guid>? _callbackTaskSource;
	internal User? User;
	private readonly BlockingCollection<(string, User)> _aimlNotificationQueue = [];
	private Simulation? _simulation;

	/// <summary>Returns whether this instance is connected to the game.</summary>
	[PublicAPI] public bool IsConnected { get; private set; }
	/// <summary>Returns whether event callbacks have been enabled.</summary>
	/// <seealso cref="EnableCallbacks"/>
	[PublicAPI] public bool CallbacksEnabled { get; private set; }

	private const int Port = 8086;

	static DefuserConnector() {
		TimerReader = GetComponentReader<Timer>();
		ComponentReadersByName = ComponentReaders.ToDictionary(e => e.Key.Name, e => e.Value, StringComparer.OrdinalIgnoreCase);
	}

	public DefuserConnector() => _instance ??= this;

	~DefuserConnector() => Dispose();

	/// <summary>Closes the connection to the game and releases resources used by this <see cref="DefuserConnector"/>.</summary>
	public void Dispose() {
		_connection?.tcpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>Starts processing callbacks to the AIML bot.</summary>
	public void EnableCallbacks() {
		CallbacksEnabled = true;
		var thread = new Thread(RunCallbackThread) { IsBackground = true, Name = $"{nameof(DefuserConnector)} callback thread" };
		thread.Start();
	}

	private void RunCallbackThread() {
		while (true) {
			var (s, user) = _aimlNotificationQueue.Take();
			user.Postback(s);
		}
	}

	/// <summary>Connects to the game or initialises a simulation.</summary>
	public async Task ConnectAsync(ILoggerFactory loggerFactory, bool useSimulation = false) {
		if (IsConnected) throw new InvalidOperationException("Cannot connect while already connected.");
		if (useSimulation) {
			_simulation = new(loggerFactory);
			_simulation.Postback += (_, m) => User?.Postback(m);
		} else {
			var tcpClient = new TcpClient();
			await tcpClient.ConnectAsync(new(IPAddress.Loopback, Port));
			var stream = tcpClient.GetStream();
			var reader = new DefuserMessageReader(stream, 14745612);
			reader.Disconnected += Reader_Disconnected;
			reader.MessageReceived += Reader_MessageReceived;
			_connection = (tcpClient, reader, new(stream, new byte[1024]));
		}
		IsConnected = true;
	}

	private void Reader_Disconnected(object? sender, DisconnectedEventArgs e) => SendAimlNotification($"OOB DefuserSocketError {e.Exception?.Message ?? "nil"}");
#pragma warning disable CS0618 // TODO: Obsolete message types may be removed later.
	private void Reader_MessageReceived(object? sender, DefuserMessageEventArgs e) {
		switch (e.Message) {
			case LegacyEventMessage legacyEventMessage:
				SendAimlNotification($"OOB DefuserSocketMessage {legacyEventMessage.Event}");
				break;
			case ScreenshotResponseMessage screenshotResponseMessage:
				if (_screenshotTaskSource is null) break;
				var image = Image.LoadPixelData<Rgba32>(screenshotResponseMessage.Data.AsSpan(8, screenshotResponseMessage.PixelDataLength), screenshotResponseMessage.Width, screenshotResponseMessage.Height);
				image.Mutate(c => c.Flip(FlipMode.Vertical));
				_screenshotTaskSource.SetResult(image);
				break;
			case CheatReadResponseMessage cheatReadResponseMessage:
				_readTaskSource?.SetResult(cheatReadResponseMessage.Data);
				break;
			case CheatReadErrorMessage cheatReadErrorMessage:
				_readTaskSource?.SetException(new Exception($"Read failed: {cheatReadErrorMessage.Message}"));
				break;
			case LegacyInputCallbackMessage legacyInputCallbackMessage:
				SendAimlNotification($"OOB DefuserCallback {legacyInputCallbackMessage.Token}");
				break;
			case InputCallbackMessage inputCallbackMessage:
				_callbackTaskSource?.SetResult(inputCallbackMessage.Token);
				SendAimlNotification($"OOB DefuserCallback {inputCallbackMessage.Token:N}");
				break;
			case CheatGetModuleTypeResponseMessage cheatGetModuleTypeResponseMessage:
				_readTaskSource?.SetResult(cheatGetModuleTypeResponseMessage.ModuleType);
				break;
			case CheatGetModuleTypeErrorMessage cheatGetModuleTypeErrorMessage:
				_readTaskSource?.SetException(new Exception($"Read failed: {cheatGetModuleTypeErrorMessage.Message}"));
				break;
			case GameStartMessage:
				SendAimlNotification("OOB GameStart");
				break;
			case GameEndMessage:
				SendAimlNotification("OOB GameEnd");
				break;
			case NewBombMessage newBombMessage:
				SendAimlNotification($"OOB NewBomb {newBombMessage.NumStrikes} {newBombMessage.NumSolvableModules} {newBombMessage.NumNeedyModules} {Math.Floor(newBombMessage.Time.TotalSeconds)}");
				break;
			case StrikeMessage strikeMessage:
				SendAimlNotification($"OOB Strike {strikeMessage.Slot.Bomb} {strikeMessage.Slot.Face} {strikeMessage.Slot.X} {strikeMessage.Slot.Y}");
				break;
			case AlarmClockChangeMessage alarmClockChangeMessage:
				SendAimlNotification($"OOB AlarmClockChange {alarmClockChangeMessage.On}");
				break;
			case LightsStateChangeMessage lightsStateChangeMessage:
				SendAimlNotification($"OOB LightsChange {lightsStateChangeMessage.On}");
				break;
			case NeedyStateChangeMessage needyStateChangeMessage:
				SendAimlNotification($"OOB NeedyStateChange {needyStateChangeMessage.Slot.Bomb} {needyStateChangeMessage.Slot.Face} {needyStateChangeMessage.Slot.X} {needyStateChangeMessage.Slot.Y} {needyStateChangeMessage.State}");
				break;
			case BombExplodeMessage:
				SendAimlNotification("OOB BombExplode");
				break;
			case BombDefuseMessage bombDefuseMessage:
				SendAimlNotification($"OOB BombDefuse {bombDefuseMessage.Bomb}");
				break;
		}
	}
#pragma warning restore CS0618 // Type or member is obsolete

	private void SendAimlNotification(string message) {
		if (CallbacksEnabled)
			_aimlNotificationQueue.Add((message, User ?? throw new InvalidOperationException("No connecting user")));
	}

	private void SendMessage(IDefuserMessage message) {
		if (_connection is null) throw new InvalidOperationException("Not connected.");
		_connection.Value.writer.Write(message);
	}

#if DEBUG
	private static void SaveDebugImage(Image image, string category) {
		var file = Path.Combine(Path.GetTempPath(), "KtaneDefuserDebug", $"{category}.{DateTime.Now:yyyy-MM-dd.HH-mm-ss.ffffff}.png");
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		image.SaveAsPng(file);
	}

	public static void SaveTrainingImage(Image image, string category) {
		var file = Path.Combine("KtaneDefuserTraining", category, $"{DateTime.Now:yyyy-MM-dd.HH-mm-ss.ffffff}.png");
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		image.SaveAsPng(file);
	}
#endif

	/// <summary>Synchronously retrieves a screenshot of the game.</summary>
	public Image<Rgba32> TakeScreenshot() => TakeScreenshotAsync().Result;
	/// <summary>Asynchronously retrieves a screenshot of the game.</summary>
	public async Task<Image<Rgba32>> TakeScreenshotAsync() {
		if (_simulation is not null)
			return Simulation.DummyScreenshot;
		if (_screenshotTaskSource is not null) throw new InvalidOperationException("Already taking a screenshot.");
		try {
			_screenshotTaskSource = new();
			SendMessage(new ScreenshotCommandMessage());
			return await _screenshotTaskSource.Task;
		} finally {
			_screenshotTaskSource = null;
		}
	}

	/// <summary>Sends the specified controller inputs to the game.</summary>
	[Obsolete($"String input commands are being replaced with {nameof(IInputAction)}.")]
	public void SendInputs(string inputs) {
		if (_simulation is not null) throw new NotSupportedException("String input commands are not supported in the simulation.");
		try {
			SendMessage(new LegacyCommandMessage($"input {inputs}"));
		} catch (Exception ex) {
			User?.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	}
	/// <summary>Sends the specified controller inputs to the game.</summary>
	public void SendInputs(params IInputAction[] actions) => SendInputs((IEnumerable<IInputAction>) actions);
	/// <summary>Sends the specified controller inputs to the game.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		if (_simulation is not null)
			_simulation.SendInputs(actions);
		else
			SendMessage(new InputCommandMessage(actions));
	}

	public async Task SendInputsAsync(params IEnumerable<IInputAction> actions) {
		if (_callbackTaskSource is not null) throw new InvalidOperationException("Already sending inputs.");
		var token = Guid.NewGuid();
		SendMessage(new InputCommandMessage([.. actions, new CallbackAction(token)]));
		try {
			while (true) {
				_callbackTaskSource = new();
				var token2 = await _callbackTaskSource.Task;
				if (token2 == token) return;
			}
		} finally {
			_callbackTaskSource = null;
		}
	}

	/// <summary>Cancels any queued input actions.</summary>
	public void CancelInputs() {
		if (_simulation is not null)
			_simulation.CancelInputs();
		else
			SendMessage(new CancelCommandMessage());
	}

	/// <summary>If in a simulation, solves the current module.</summary>
	public void CheatSolve() {
		if (_simulation is null)
			throw new InvalidOperationException("No simulation active");
		_simulation.Solve();
	}

	/// <summary>If in a simulation, triggers a strike on the current module.</summary>
	public void CheatStrike() {
		if (_simulation is null)
			throw new InvalidOperationException("No simulation active");
		_simulation.Strike();
	}

	/// <summary>If in a simulation, triggers the alarm clock.</summary>
	public void CheatTriggerAlarmClock() {
		if (_simulation is null)
			throw new InvalidOperationException("No simulation active");
		_simulation.SetAlarmClock(true);
	}

	private static bool IsBombBacking(HsvColor hsv) => hsv is { H: >= 180 and < 225, S: < 0.35f, V: >= 0.35f };

	/// <summary>Returns the distance by which side widget polygons should be adjusted, in pixels to the right.</summary>
	public int GetSideWidgetAdjustment(Image<Rgba32> screenshotBitmap) {
		if (_simulation is not null)
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

	/// <summary>Returns the lights state in the specified screenshot.</summary>
	public LightsState GetLightsState(Image<Rgba32> screenshot) => _simulation is not null ? LightsState.On : ImageUtils.GetLightsState(screenshot);

	/// <summary>Returns the light state of the module in the specified polygon.</summary>
	public ModuleStatus GetModuleStatus(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral, ComponentReader reader)
		=> _simulation?.GetLightState(quadrilateral) ?? reader.GetStatus(screenshotBitmap, quadrilateral, ImageUtils.GetLightsState(screenshotBitmap));

	/// <summary>Returns the <see cref="ComponentReader"/> singleton instance of the specified type.</summary>
	public static T GetComponentReader<T>() where T : ComponentReader => (T) ComponentReaders[typeof(T)];

	/// <summary>Identifies the component in the specified polygon and returns the corresponding <see cref="ComponentReader"/> instance, or <see langword="null"/> if it is a blank component.</summary>
	public ComponentReader? GetComponentReader(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral) {
		if (_simulation is not null)
			return _simulation.GetComponentReader(quadrilateral);

		// Extract an image of the component from the screenshot.
		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap, quadrilateral, InterpolationMode.NearestNeighbour, new(224, 224));
		
		// Identify the component.
		var name = ComponentIdentifier.Identify(bitmap);
		return name is not null ? ComponentReadersByName[name] : null;
	}

	/// <summary>Retrieves the type of the component in the specified polygon and returns the corresponding <see cref="ComponentReader"/> instance, or <see langword="null"/> if it is a blank component or the timer.</summary>
	public ComponentReader? CheatGetComponentReader(Slot slot) {
		if (_simulation is not null)
			return _simulation.GetComponentReader(slot);

		_readTaskSource = new();
		SendMessage(new CheatGetModuleTypeCommandMessage(slot));
		var name = _readTaskSource.Task.Result;
		return !string.IsNullOrEmpty(name) && typeof(ComponentReader).Assembly.GetType($"{nameof(KtaneDefuserConnector)}.{nameof(Components)}.{name}", false, true) is { } t ? ComponentReaders[t] : null;
	}

	public async Task<string?> CheatGetComponentNameAsync(Slot slot) {
		if (_simulation is not null)
			return _simulation.GetComponentReader(slot)?.GetType().Name;

		_readTaskSource = new();
		SendMessage(new CheatGetModuleTypeCommandMessage(slot));
		return await _readTaskSource.Task;
	}

	/// <summary>Identifies the widget in the specified polygon and returns the corresponding <see cref="WidgetReader"/> instance, or <see langword="null"/> if no widget is found there.</summary>
	public WidgetReader? GetWidgetReader(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral) {
		if (_simulation is not null)
			return _simulation.GetWidgetReader(quadrilateral);

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap, quadrilateral, InterpolationMode.NearestNeighbour);
		var pixelCounts = WidgetReader.GetPixelCounts(bitmap, 0);
		return pixelCounts switch {
			{ Yellow: >= 1000 } => BatteryHolderReader,
			{ Grey: >= 5000 } => PortPlateReader,
			{ Red: >= 10000 } or { Red: >= 5000, White: >= 5000 } => pixelCounts.Red >= pixelCounts.White * 2 ? IndicatorReader : SerialNumberReader,
			_ => null
		};
	}

	/// <summary>Reads component data from the module in the specified polygon using the specified <see cref="ComponentReader"/>.</summary>
	public T ReadComponent<T>(Image<Rgba32> screenshot, LightsState lightsState, ComponentReader<T> reader, Quadrilateral quadrilateral) where T : ComponentReadData {
		if (_simulation is not null)
			return _simulation.ReadComponent<T>(quadrilateral);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour, new(Resolution, Resolution));
#if DEBUG
		Task.Run(() => SaveDebugImage(image, reader.Name));
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, lightsState, ref debugImage);
	}

	/// <summary>Reads widget data from the widget in the specified polygon using the specified <see cref="WidgetReader"/>.</summary>
	public T ReadWidget<T>(Image<Rgba32> screenshot, LightsState lightsState, WidgetReader<T> reader, Quadrilateral quadrilateral) where T : notnull {
		if (_simulation is not null)
			return _simulation.ReadWidget<T>(quadrilateral);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour);
#if DEBUG
		Task.Run(() => SaveDebugImage(image, reader.Name));
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, lightsState, ref debugImage);
	}

	/// <summary>Retrieves the value of an internal field in the component in the specified slot.</summary>
	/// <param name="slot">The position of the component slot to read.</param>
	/// <param name="members">
	/// A list of chained specifiers indicating what to read.
	/// This may be a field or property name, an <see cref="IEnumerable{T}"/> index in square brackets, or a component type name in braces.
	/// If the component type name is prefixed with a <c>*</c>, it instead reads a collection of that type of component in the current game object and its descendants.
	/// </param>
	public string? CheatRead(Slot slot, params string[] members) {
		if (_simulation is not null)
			throw new NotImplementedException();

		_readTaskSource = new();
		SendMessage(new CheatReadCommandMessage(slot, members));
		return _readTaskSource.Task.Result;
	}

	[Obsolete($"This method is being replaced with {nameof(ReadComponent)} and {nameof(ReadWidget)}.")]
	internal string? Read(string readerName, Image<Rgba32> screenshot, Quadrilateral quadrilateral) {
		if (_simulation is not null)
			return typeof(ComponentReader).Assembly.GetType($"{nameof(KtaneDefuserConnector)}.{nameof(Components)}.{readerName}", false, true) is { } t
				? _simulation.ReadModule(t.Name, quadrilateral)
				: typeof(WidgetReader).Assembly.GetType($"{nameof(KtaneDefuserConnector)}.{nameof(Widgets)}.{readerName}", false, true) is { } t2
				? _simulation.ReadWidget(t2.Name, quadrilateral)
				: throw new ArgumentException($"No such command, module or widget is known: {readerName}");

		var lightsState = ImageUtils.GetLightsState(screenshot);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour, new(Resolution, Resolution));
#if DEBUG
		SaveDebugImage(image, readerName);
#endif

		var args = new object?[] { image, lightsState, null };
		var type = typeof(ComponentReader).Assembly.GetType($"{nameof(KtaneDefuserConnector)}.{nameof(Components)}.{readerName}");
		if (type is not null)
			return type.GetMethod(nameof(ComponentReader<>.Process), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ComponentReaders[type], args)!.ToString();

		object reader = readerName switch {
			nameof(BatteryHolder) => BatteryHolderReader,
			nameof(Indicator) => IndicatorReader,
			nameof(PortPlate) => PortPlateReader,
			nameof(SerialNumber) => SerialNumberReader,
			_ => throw new ArgumentException($"No such command, component or widget is known: {readerName}")
		};
		return reader.GetType().GetMethod(nameof(ComponentReader<>.Process), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(reader, args)!.ToString();
	}
}
