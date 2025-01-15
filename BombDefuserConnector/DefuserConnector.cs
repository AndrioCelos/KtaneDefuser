using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Aiml;
using BombDefuserConnectorApi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
/// <summary>Integrates a bot with Keep Talking and Nobody Explodes.</summary>
public class DefuserConnector : IDisposable {
	private static readonly Dictionary<Type, ComponentReader> componentReaders
		= (from t in typeof(ComponentReader).Assembly.GetTypes() where !t.IsAbstract && typeof(ComponentReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (ComponentReader) Activator.CreateInstance(t)!);
	private static readonly Dictionary<Type, WidgetReader> widgetReaders
		= (from t in typeof(WidgetReader).Assembly.GetTypes() where !t.IsAbstract && typeof(WidgetReader).IsAssignableFrom(t) select t).ToDictionary(t => t, t => (WidgetReader) Activator.CreateInstance(t)!);

	private (TcpClient tcpClient, DefuserMessageReader reader, DefuserMessageWriter writer)? connection;

	private TaskCompletionSource<Image<Rgba32>>? screenshotTaskSource;
	private TaskCompletionSource<string?>? readTaskSource;
	internal User? user;
	private readonly BlockingCollection<(string, User)> aimlNotificationQueue = [];
	private Simulation? simulation;

	private static DefuserConnector? instance;
	public static DefuserConnector Instance => instance ?? throw new InvalidOperationException("Service not yet initialised");
	/// <summary>Returns the <see cref="ComponentReader"/> instance representing the timer.</summary>
	public static Components.Timer TimerReader { get; }

	/// <summary>Returns whether this instance is connected to the game.</summary>
	public bool IsConnected { get; private set; }
	/// <summary>Returns whether event callbacks have been enabled.</summary>
	/// <seealso cref="EnableCallbacks"/>
	public bool CallbacksEnabled { get; private set; }

	private const int PORT = 8086;

	static DefuserConnector() => TimerReader = GetComponentReader<Components.Timer>();

	public DefuserConnector() => instance ??= this;

	~DefuserConnector() => this.Dispose();

	/// <summary>Closes the connection to the game and releases resources used by this <see cref="DefuserConnector"/>.</summary>
	public void Dispose() {
		this.connection?.tcpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>Starts processing callbacks to the AIML bot.</summary>
	public void EnableCallbacks() {
		this.CallbacksEnabled = true;
		var thread = new Thread(this.RunCallbackThread) { IsBackground = true, Name = $"{nameof(DefuserConnector)} callback thread" };
		thread.Start();
	}

	private void RunCallbackThread() {
		while (true) {
			var (s, user) = this.aimlNotificationQueue.Take();
			user.Postback(s);
		}
	}

	/// <summary>Connects to the game or initialises a simulation.</summary>
	public async Task ConnectAsync(bool simulation) {
		if (this.IsConnected) throw new InvalidOperationException("Cannot connect while already connected.");
		if (simulation) {
			this.simulation = new();
			this.simulation.Postback += (s, m) => this.user?.Postback(m);
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
#pragma warning disable CS0618 // TODO: Obsolete message types may be removed later.
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
#pragma warning restore CS0618 // Type or member is obsolete

	private void SendAimlNotification(string message) {
		if (this.CallbacksEnabled)
			this.aimlNotificationQueue.Add((message, this.user ?? throw new InvalidOperationException("No connecting user")));
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

	/// <summary>Synchronously retrieves a screenshot of the game.</summary>
	public Image<Rgba32> TakeScreenshot() => this.TakeScreenshotAsync().Result;
	/// <summary>Asynchronously retrieves a screenshot of the game.</summary>
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

	/// <summary>Sends the specified controller inputs to the game.</summary>
	[Obsolete($"String input commands are being replaced with {nameof(IInputAction)}.")]
	public void SendInputs(string inputs) {
		if (this.simulation is not null)
			throw new NotImplementedException();
		else {
			try {
				this.SendMessage(new LegacyCommandMessage($"input {inputs}"));
			} catch (Exception ex) {
				this.user?.Postback($"OOB DefuserSocketError {ex.Message}");
			}
		}
	}
	/// <summary>Sends the specified controller inputs to the game.</summary>
	public void SendInputs(params IInputAction[] actions) => this.SendInputs((IEnumerable<IInputAction>) actions);
	/// <summary>Sends the specified controller inputs to the game.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		if (this.simulation is not null)
			this.simulation.SendInputs(actions);
		else
			this.SendMessage(new InputCommandMessage(actions));
	}

	/// <summary>Cancels any queued input actions.</summary>
	public void CancelInputs() {
		if (this.simulation is not null)
			this.simulation.CancelInputs();
		else
			this.SendMessage(new CancelCommandMessage());
	}

	/// <summary>If in a simulation, solves the current module.</summary>
	public void CheatSolve() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.Solve();
	}

	/// <summary>If in a simulation, triggers a strike on the current module.</summary>
	public void CheatStrike() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.Strike();
	}

	/// <summary>If in a simulation, triggers the alarm clock.</summary>
	public void CheatTriggerAlarmClock() {
		if (this.simulation is null)
			throw new InvalidOperationException("No simulation active");
		this.simulation.SetAlarmClock(true);
	}

	private static bool IsBombBacking(HsvColor hsv) => hsv.H is >= 180 and < 225 && hsv.S < 0.35f && hsv.V >= 0.35f;

	/// <summary>Returns the distance by which side widget polygons should be adjusted, in pixels to the right.</summary>
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

	/// <summary>Returns the lights state in the specified screenshot.</summary>
	public LightsState GetLightsState(Image<Rgba32> screenshot) => this.simulation is not null ? LightsState.On : ImageUtils.GetLightsState(screenshot);

	/// <summary>Returns the light state of the module in the specified polygon.</summary>
	public ModuleLightState GetModuleLightState(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral)
		=> this.simulation is not null
			? this.simulation.GetLightState(quadrilateral)
			: ImageUtils.GetLightState(screenshotBitmap, quadrilateral);

	/// <summary>Returns the <see cref="ComponentReader"/> singleton instance of the specified type.</summary>
	public static T GetComponentReader<T>() where T : ComponentReader => (T) componentReaders[typeof(T)];

	/// <summary>Identifies the component in the specified polygon and returns the corresponding <see cref="ComponentReader"/> instance, or <see langword="null"/> if it is a blank component.</summary>
	public ComponentReader? GetComponentReader(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral) {
		if (this.simulation is not null)
			return this.simulation.GetComponentReader(quadrilateral);

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap, quadrilateral, InterpolationMode.NearestNeighbour);

		if (ImageUtils.CheckForBlankComponent(bitmap))
			return null;

		var frameType = ImageUtils.GetComponentFrame(bitmap);

		var ratings = new List<(ComponentReader reader, float rating)>();
		foreach (var reader in componentReaders.Values) {
			if (reader.FrameType == frameType)
				ratings.Add((reader, reader.IsModulePresent(bitmap)));
		}

		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].reader;
	}

	/// <summary>Retrieves the type of the component in the specified polygon and returns the corresponding <see cref="ComponentReader"/> instance, or <see langword="null"/> if it is a blank component or the timer.</summary>
	public ComponentReader? CheatGetComponentReader(Slot slot) {
		if (this.simulation is not null)
			return this.simulation.GetComponentReader(slot);

		this.readTaskSource = new();
		this.SendMessage(new CheatGetModuleTypeCommandMessage(slot));
		var name = this.readTaskSource.Task.Result;
		return !string.IsNullOrEmpty(name) && typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{name}", false, true) is Type t ? componentReaders[t] : null;
	}

	/// <summary>Identifies the widget in the specified polygon and returns the corresponding <see cref="WidgetReader"/> instance, or <see langword="null"/> if no widget is found there.</summary>
	public WidgetReader? GetWidgetReader(Image<Rgba32> screenshotBitmap, Quadrilateral quadrilateral) {
		if (this.simulation is not null)
			return this.simulation.GetWidgetReader(quadrilateral);

		var bitmap = ImageUtils.PerspectiveUndistort(screenshotBitmap, quadrilateral, InterpolationMode.NearestNeighbour);
		var pixelCounts = WidgetReader.GetPixelCounts(bitmap, 0);

		var ratings = new List<(WidgetReader reader, float rating)>();
		foreach (var reader in widgetReaders.Values) {
			ratings.Add((reader, reader.IsWidgetPresent(bitmap, 0, pixelCounts)));
		}
		ratings.Sort((e1, e2) => e2.rating.CompareTo(e1.rating));
		return ratings[0].rating >= 0.25f ? ratings[0].reader : null;
	}

	/// <summary>Reads component data from the module in the specified polygon using the specified <see cref="ComponentReader"/>.</summary>
	public T ReadComponent<T>(Image<Rgba32> screenshot, LightsState lightsState, ComponentReader<T> reader, Quadrilateral quadrilateral) where T : notnull {
		if (this.simulation is not null)
			return this.simulation.ReadComponent<T>(quadrilateral);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, reader.Name);
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, lightsState, ref debugImage);
	}

	/// <summary>Reads widget data from the widget in the specified polygon using the specified <see cref="WidgetReader"/>.</summary>
	public T ReadWidget<T>(Image<Rgba32> screenshot, LightsState lightsState, WidgetReader<T> reader, Quadrilateral quadrilateral) where T : notnull {
		if (this.simulation is not null)
			return this.simulation.ReadWidget<T>(quadrilateral);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour);
#if DEBUG
		SaveDebugImage(image, reader.Name);
#endif
		Image<Rgba32>? debugImage = null;
		return reader.Process(image, lightsState, ref debugImage);
	}

	/// <summary>Retrieves the value of an internal field in the module in the specified slot.</summary>
	/// <param name="members">
	/// A list of chained specifiers indicating what to read.
	/// May be a field or property name, an <see cref="IEnumerable{T}"/> index in square brackets, or a component type name in braces.
	/// If the component type name is prefixed with a <c>*</c>, it instead reads a collection of that type of component in the current game object and its descendants.
	/// </param>
	public string? CheatRead(Slot slot, params string[] members) {
		if (this.simulation is not null)
			throw new NotImplementedException();

		this.readTaskSource = new();
		this.SendMessage(new CheatReadCommandMessage(slot, members));
		return this.readTaskSource.Task.Result;
	}

	[Obsolete($"This method is being replaced with {nameof(ReadComponent)} and {nameof(ReadWidget)}.")]
	internal string? Read(string readerName, Image<Rgba32> screenshot, Quadrilateral quadrilateral) {
		if (this.simulation is not null)
			return typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{readerName}", false, true) is Type t
				? this.simulation.ReadModule(t.Name, quadrilateral)
				: typeof(WidgetReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Widgets)}.{readerName}", false, true) is Type t2
				? this.simulation.ReadWidget(t2.Name, quadrilateral)
				: throw new ArgumentException($"No such command, module or widget is known: {readerName}");

		var lightsState = ImageUtils.GetLightsState(screenshot);
		var image = ImageUtils.PerspectiveUndistort(screenshot, quadrilateral, InterpolationMode.NearestNeighbour);
		Image<Rgba32>? debugImage = null;
#if DEBUG
		SaveDebugImage(image, readerName);
#endif

		var type = typeof(ComponentReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Components)}.{readerName}");
		if (type is not null)
			return componentReaders[type].ProcessNonGeneric(image, lightsState, ref debugImage)?.ToString();

		type = typeof(WidgetReader).Assembly.GetType($"{nameof(BombDefuserConnector)}.{nameof(Widgets)}.{readerName}");
		return type is not null
			? widgetReaders[type].ProcessNonGeneric(image, lightsState, ref debugImage)?.ToString()
			: throw new ArgumentException($"No such command, component or widget is known: {readerName}");
	}
}
