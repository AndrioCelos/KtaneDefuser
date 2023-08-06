using System.ComponentModel;
using Aiml;

namespace BombDefuserScripts;
[AimlInterface]
internal class NeedyModules {
	[AimlCategory("OOB DefuserSocketMessage NeedyStateChanged * * * *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void NeedyStateChange(AimlAsyncContext context, int faceNum, int x, int y, NeedyState newState) {
		var module = GameState.Current.Faces[faceNum][x, y];
		if (module?.Script is ModuleScript script) {
			script.NeedyState = newState;
			script.NeedyStateChanged(context, newState);
		} else {
			// This is a module we haven't seen yet; deal with it when we've seen what it is and initialised the script.
			lock (GameState.Current.UnknownNeedyStates)
				GameState.Current.UnknownNeedyStates[new(faceNum, x, y)] = newState;
		}
	}
}
