using System.Reflection;

namespace BombDefuserScripts;
public abstract class ModuleScript {
	private static readonly Dictionary<Type, (Type scriptType, string topic)> Scripts = new(from t in typeof(ModuleScript).Assembly.GetTypes() where t.BaseType is Type t2 && t2.IsGenericType && t2.GetGenericTypeDefinition() == typeof(ModuleScript<>)
																							select new KeyValuePair<Type, (Type, string)>(t.BaseType!.GenericTypeArguments[0], (t, t.GetCustomAttribute<AimlInterfaceAttribute>()?.Topic ?? throw new InvalidOperationException($"Missing topic on {t.Name}"))));
	private static readonly MethodInfo CreateMethodBase = typeof(ModuleScript).GetMethod(nameof(CreateInternal), BindingFlags.NonPublic | BindingFlags.Static)!;
	private static readonly Dictionary<Type, MethodInfo> CreateMethodCache = new();

	private protected string? topic;
	public string Topic => topic ?? throw new InvalidOperationException("Script not yet initialised");

	public abstract string IndefiniteDescription { get; }
	public virtual PriorityCategory PriorityCategory => PriorityCategory.None;

	public int ModuleIndex { get; internal set; }
	public NeedyState NeedyState { get; internal set; }

	public static ModuleScript Create(ComponentReader reader) {
		var readerType = reader.GetType();
		if (!Scripts.TryGetValue(readerType, out var entry))
			return new UnknownModuleScript();

		var (scriptType, topic) = entry;
		if (!CreateMethodCache.TryGetValue(scriptType, out var method)) {
			method = CreateMethodBase.MakeGenericMethod(scriptType, readerType);
			CreateMethodCache[scriptType] = method;
		}
		return (ModuleScript) method.Invoke(null, new object[] { topic })!;
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
	/// <summary>Called when the state of the needy module has changed.</summary>
	protected internal virtual void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) { }

	protected static Task<Interrupt> CurrentModuleInterruptAsync(AimlAsyncContext context) => (GameState.Current.CurrentModule ?? throw new InvalidOperationException("No current module")).Script.ModuleInterruptAsync(context, true);
	protected static Task<Interrupt> CurrentModuleInterruptAsync(AimlAsyncContext context, bool waitForFocus) => (GameState.Current.CurrentModule ?? throw new InvalidOperationException("No current module")).Script.ModuleInterruptAsync(context, true);

	protected Task<Interrupt> ModuleInterruptAsync(AimlAsyncContext context) => this.ModuleInterruptAsync(context, true);
	protected async Task<Interrupt> ModuleInterruptAsync(AimlAsyncContext context, bool waitForFocus) {
		var interrupt = await Interrupt.EnterAsync(context);
		await Utils.SelectModuleAsync(interrupt, this.ModuleIndex, waitForFocus);
		return interrupt;
	}
}

public abstract class ModuleScript<TReader> : ModuleScript where TReader : ComponentReader {
	/// <summary>Returns the <see cref="ComponentReader"/> instance matching this module type.</summary>
	protected static TReader Reader => DefuserConnector.GetComponentReader<TReader>();
}

internal class UnknownModuleScript : ModuleScript {
	public override string IndefiniteDescription => "an unknown module";
}

public enum PriorityCategory {
	None,
	Needy
}

public enum NeedyState {
	InitialSetup,
	AwaitingActivation,
	Running,
	Cooldown,
	Terminated,
	BombComplete
}