using System.ComponentModel;

namespace BombDefuserScripts;
[AimlInterface]
internal class NeedyModules {
	[AimlCategory("OOB DefuserSocketMessage NeedyStateChanged * * * *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void NeedyStateChange(AimlAsyncContext context, int faceNum, int x, int y, NeedyState newState) {
		var module = GameState.Current.Faces[faceNum][x, y];
		if (module?.Script is not ModuleScript script) return;
		script.NeedyState = newState;
		script.NeedyStateChanged(context, newState);
	}
}
