using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AngelAiml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector;
/// <summary>Integrates an AIML bot with Keep Talking and Nobody Explodes.</summary>
public class KtaneDefuserAimlExtension : IAimlExtension {
	private readonly DefuserConnector connector = new();
	private static readonly Dictionary<string, Image<Rgba32>> cachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private static readonly Queue<string> cachedScreenshotIds = new();

	public void Initialise() {
		connector.EnableCallbacks();
		AimlLoader.AddCustomOobHandler("takescreenshot", OobAction(async (c, e, r) => {
			var token = e.Attribute("token")?.Value ?? e.Element("token")?.Value ?? "nil";
			var image = await c.TakeScreenshotAsync();
			var newId = Guid.NewGuid().ToString("N");
			lock (cachedScreenshots) {
				if (cachedScreenshotIds.Count >= 100) {
					var toRemove = cachedScreenshotIds.Dequeue();
					cachedScreenshots[toRemove].Dispose();
					cachedScreenshots.Remove(toRemove);
				}
				cachedScreenshotIds.Enqueue(newId);
				cachedScreenshots[newId] = image;
			}
			r.User.Postback($"OOB ScreenshotReady {token} {newId}");
		}));
		AimlLoader.AddCustomOobHandler("tasconnect", OobAction(async (c, e, r) => {
			c.user = r.User;
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
	private OobHandler OobAction(Func<DefuserConnector, XElement, Response, Task> action) => async (e, r) => {
		try {
			await action.Invoke(connector, e, r);
		} catch (Exception ex) {
			r.User.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	};

#pragma warning disable CS0618 // TODO: Obsolete message types may be removed later.
	public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) {
		var tokens = text.Split();
		switch (tokens[0].ToLower()) {
			case "getsidewidgetadjustment": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				return connector.GetSideWidgetAdjustment(screenshotBitmap).ToString();
			}
			case "identifymodule": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return connector.GetComponentReader(screenshotBitmap, quadrilateral)?.GetType().Name ?? "nil";
			}
			case "identifywidget": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return connector.GetWidgetReader(screenshotBitmap, quadrilateral)?.GetType().Name ?? "nil";
			}
			case "getlightstate": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return connector.GetModuleLightState(screenshotBitmap, quadrilateral).ToString();
			}
			case "read": {
				return connector.CheatRead(new(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])), tokens.Skip(5).ToArray()) ?? "nil";
			}
			case "getmodulename": {
				return connector.CheatGetComponentReader(new(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])))?.GetType().Name ?? "nil";
			}
			default: {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return connector.Read(tokens[0], screenshotBitmap, quadrilateral) ?? "nil";
			}
		}
	}
#pragma warning restore CS0618 // Type or member is obsolete
}
