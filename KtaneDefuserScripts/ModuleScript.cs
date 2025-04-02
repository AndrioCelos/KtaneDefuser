using System.Reflection;
using KtaneDefuserConnector;
using KtaneDefuserConnectorApi;

namespace KtaneDefuserScripts;
/// <summary>Provides the script and variables associated with a specific module.</summary>
public abstract class ModuleScript {
	protected readonly ILogger Logger;

	private static readonly Dictionary<Type, (Type scriptType, string topic)> Scripts = new(from t in typeof(ModuleScript).Assembly.GetTypes() where t.BaseType is Type t2 && t2.IsGenericType && t2.GetGenericTypeDefinition() == typeof(ModuleScript<>)
																							select new KeyValuePair<Type, (Type, string)>(t.BaseType!.GenericTypeArguments[0], (t, t.GetCustomAttribute<AimlInterfaceAttribute>()?.Topic ?? throw new InvalidOperationException($"Missing topic on {t.Name}"))));
	private static readonly MethodInfo CreateMethodBase = typeof(ModuleScript).GetMethod(nameof(CreateInternal), BindingFlags.NonPublic | BindingFlags.Static)!;
	private static readonly Dictionary<Type, MethodInfo> CreateMethodCache = [];

	private protected string? topic;
	/// <summary>Returns the AIML topic associated with this script.</summary>
	public string Topic => this.topic ?? throw new InvalidOperationException("Script not yet initialised");

	/// <summary>When overridden, returns a string used to describe an instance of the module to the user.</summary>
	public abstract string IndefiniteDescription { get; }
	/// <summary>When overridden, returns a value indicating which priority category this module belongs to.</summary>
	public virtual PriorityCategory PriorityCategory => PriorityCategory.None;

	/// <summary>The index of the module handled by this instance.</summary>
	public int ModuleIndex { get; internal set; }
	/// <summary>If this instance is handling a needy module, returns the module's needy state.</summary>
	public NeedyState NeedyState { get; internal set; }

	public static IReadOnlyCollection<Type> ScriptTypes => Scripts.Keys;

	protected ModuleScript() => this.Logger = GameState.Current.LoggerFactory.CreateLogger(this.GetType().Name);

	internal static ModuleScript Create(ComponentReader reader) {
		var readerType = reader.GetType();
		if (!Scripts.TryGetValue(readerType, out var entry))
			return new UnknownModuleScript();

		var (scriptType, topic) = entry;
		if (!CreateMethodCache.TryGetValue(scriptType, out var method)) {
			method = CreateMethodBase.MakeGenericMethod(scriptType, readerType);
			CreateMethodCache[scriptType] = method;
		}
		return (ModuleScript) method.Invoke(null, [topic])!;
	}

	private static TScript CreateInternal<TScript, TReader>(string topic) where TScript : ModuleScript<TReader>, new() where TReader : ComponentReader
		=> new() { topic = topic };

	/// <summary>Called when the script is initialised at the start of the game.</summary>
	protected internal virtual void Initialise(AimlAsyncContext context) { }
	/// <summary>Called when the expert has chosen to work on this module.</summary>
	protected internal virtual void Started(AimlAsyncContext context) { }
	/// <summary>Called when the expert has ceased to work on this module.</summary>
	protected internal virtual void Stopped(AimlAsyncContext context) { }
	/// <summary>Called when the module has been fully selected and focused in-game.</summary>
	protected internal virtual void ModuleSelected(AimlAsyncContext context) { }
	/// <summary>Called when the module has been deselected in-game.</summary>
	protected internal virtual void ModuleDeselected(AimlAsyncContext context) { }
	/// <summary>Called when a strike occurs on this module.</summary>
	protected internal virtual void Strike(AimlAsyncContext context) { }
	/// <summary>Called when the state of the needy module has changed.</summary>
	protected internal virtual void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) { }

	/// <summary>Enters a new interrupt, ensuring that the current module is focused before completing.</summary>
	protected static Task<Interrupt> CurrentModuleInterruptAsync(AimlAsyncContext context) => (GameState.Current.CurrentModule ?? throw new InvalidOperationException("No current module")).Script.ModuleInterruptAsync(context, true);
	/// <summary>Enters a new interrupt, ensuring that the current module is focused before completing.</summary>
	/// <param name="waitForFocus">Whether to also wait until the module focusing animation has finished before completing.</param>
	protected static Task<Interrupt> CurrentModuleInterruptAsync(AimlAsyncContext context, bool waitForFocus) => (GameState.Current.CurrentModule ?? throw new InvalidOperationException("No current module")).Script.ModuleInterruptAsync(context, true);

	/// <summary>Enters a new interrupt, ensuring that this module is focused before completing.</summary>
	protected Task<Interrupt> ModuleInterruptAsync(AimlAsyncContext context) => this.ModuleInterruptAsync(context, true);
	/// <summary>Enters a new interrupt, ensuring that this module is selected before completing.</summary>
	/// <param name="waitForFocus">Whether to also wait until the module focusing animation has finished before completing.</param>
	protected async Task<Interrupt> ModuleInterruptAsync(AimlAsyncContext context, bool waitForFocus) {
		var interrupt = await Interrupt.EnterAsync(context);
		await Utils.SelectModuleAsync(interrupt, this.ModuleIndex, waitForFocus);
		return interrupt;
	}
}

/// <summary>Represents a <see cref="ModuleScript"/> that uses a specified <see cref="ComponentReader"/> type to read its module.</summary>
public abstract class ModuleScript<TReader> : ModuleScript where TReader : ComponentReader {
	/// <summary>Returns the <see cref="ComponentReader"/> instance matching this module type.</summary>
	protected static TReader Reader => KtaneDefuserConnector.DefuserConnector.GetComponentReader<TReader>();
}

internal class UnknownModuleScript : ModuleScript {
	public override string IndefiniteDescription => "an unknown module";
}

/// <summary>Indicates categories of modules that require special notice.</summary>
public enum PriorityCategory {
	/// <summary>Not a priority module.</summary>
	None,
	/// <summary>A needy module. These cannot be disarmed, but activate periodically or stay active until the bomb is defused.</summary>
	Needy
}
