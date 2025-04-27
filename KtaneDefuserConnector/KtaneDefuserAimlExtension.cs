using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using AngelAiml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector;
/// <summary>Integrates an AIML bot with Keep Talking and Nobody Explodes.</summary>
public class KtaneDefuserAimlExtension : IAimlExtension {
	private readonly DefuserConnector connector = new();
	private static readonly Dictionary<string, Image<Rgba32>> CachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private static readonly Queue<string> CachedScreenshotIds = new();

	public void Initialise() {
		connector.EnableCallbacks();
		AimlLoader.AddCustomOobHandler("takescreenshot", OobAction(async (c, e, r) => {
			var token = e.Attribute("token")?.Value ?? e.Element("token")?.Value ?? "nil";
			var image = await c.TakeScreenshotAsync();
			var newId = Guid.NewGuid().ToString("N");
			lock (CachedScreenshots) {
				if (CachedScreenshotIds.Count >= 100) {
					var toRemove = CachedScreenshotIds.Dequeue();
					CachedScreenshots[toRemove].Dispose();
					CachedScreenshots.Remove(toRemove);
				}
				CachedScreenshotIds.Enqueue(newId);
				CachedScreenshots[newId] = image;
			}
			r.User.Postback($"OOB ScreenshotReady {token} {newId}");
		}));
		AimlLoader.AddCustomOobHandler("tasconnect", OobAction(async (c, e, r) => {
			c.User = r.User;
			await c.ConnectAsync(r.Bot.LoggerFactory, e.Attribute("simulation") is not null);
			r.User.Postback("OOB DefuserSocketConnected");
		}));
#pragma warning disable CS0618 // TODO: Obsolete message types may be removed later.
		AimlLoader.AddCustomOobHandler("sendinputs", OobAction((c, e, _) => c.SendInputs(e.Value)));
#pragma warning restore CS0618 // Type or member is obsolete
		AimlLoader.AddCustomOobHandler("solve", OobAction(c => c.CheatSolve()));

		AimlLoader.AddCustomOobHandler("strike", OobAction(c => c.CheatStrike()));
		AimlLoader.AddCustomOobHandler("triggeralarmclock", OobAction(c => c.CheatTriggerAlarmClock()));
	}

	private OobHandler OobAction(Action<DefuserConnector> action) => OobAction((c, _, _) => action(c));
	private OobHandler OobAction(Action<DefuserConnector, XElement, Response> action) => (e, r) => {
		try {
			action.Invoke(connector, e, r);
		} catch (Exception ex) {
			r.User.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	};
	private OobHandler OobAction(Func<DefuserConnector, XElement, Response, Task> action) => async void (e, r) => {
		try {
			await action.Invoke(connector, e, r);
		} catch (Exception ex) {
			r.User.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	};
}
