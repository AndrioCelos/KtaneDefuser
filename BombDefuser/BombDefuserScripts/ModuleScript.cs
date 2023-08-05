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
		return (ModuleScript) method.Invoke(null, new object[] { reader, topic })!;
	}

	private static TScript CreateInternal<TScript, TReader>(TReader reader, string topic) where TScript : ModuleScript<TReader>, new() where TReader : ComponentReader
		=> new() { reader = reader, topic = topic };

	protected static T ReadCurrent<T>(ComponentReader<T> reader) where T : notnull {
		var ss = DefuserConnector.Instance.TakeScreenshot();
		return DefuserConnector.Instance.ReadComponent(ss, reader, Utils.CurrentModulePoints);
	}

	protected internal virtual void Initialise(AimlAsyncContext context) { }
	protected internal virtual void Entering(AimlAsyncContext context) { }
	protected internal virtual void ModuleSelected(AimlAsyncContext context) { }
	protected internal virtual void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) { }
}

public abstract class ModuleScript<TReader> : ModuleScript where TReader : ComponentReader {
	internal TReader? reader;
	protected TReader Reader => reader ?? throw new InvalidOperationException("Script not yet initialised");

	protected static TReader GetReader() => DefuserConnector.GetComponentReader<TReader>();
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