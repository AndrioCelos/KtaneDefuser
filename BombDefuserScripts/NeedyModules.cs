using System.ComponentModel;

namespace BombDefuserScripts;
[AimlInterface]
internal class NeedyModules {
	[AimlCategory("OOB NeedyStateChange * * * * *"), EditorBrowsable(EditorBrowsableState.Never)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The signature must be this for interoperability. This parameter may be used in a future update.")]
	public static void NeedyStateChange(AimlAsyncContext context, int bombNum, int faceNum, int x, int y, NeedyState newState) {
		var module = GameState.Current.Faces[faceNum][x, y];
		if (module?.Script is ModuleScript script) {
			script.NeedyState = newState;
			script.NeedyStateChanged(context, newState);
		} else {
			// This is a module we haven't seen yet; deal with it when we've seen what it is and initialised the script.
			lock (GameState.Current.UnknownNeedyStates)
				GameState.Current.UnknownNeedyStates[new(0, faceNum, x, y)] = newState;
		}
	}
}
