using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Aiml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
/// <summary>Integrates an AIML bot with Keep Talking and Nobody Explodes.</summary>
public class BombDefuserAimlService : IAimlExtension {
	private readonly DefuserConnector connector = new();
	private static readonly Dictionary<string, Image<Rgba32>> cachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private static readonly Queue<string> cachedScreenshotIds = new();

	public void Initialise() {
		this.connector.EnableCallbacks();
		AimlLoader.AddCustomOobHandler("takescreenshot", this.OobAction(async (c, e, r) => {
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
		AimlLoader.AddCustomOobHandler("tasconnect", this.OobAction(async (c, e, r) => {
			c.user = r.User;
			await c.ConnectAsync(e.Attribute("simulation") is not null);
			r.User.Postback("OOB DefuserSocketConnected");
		}));
		AimlLoader.AddCustomOobHandler("sendinputs", this.OobAction((c, e, r) => c.SendInputs(e.Value)));
		AimlLoader.AddCustomOobHandler("solve", this.OobAction(c => c.CheatSolve()));

		AimlLoader.AddCustomOobHandler("strike", this.OobAction(c => c.CheatStrike()));
		AimlLoader.AddCustomOobHandler("triggeralarmclock", this.OobAction(c => c.CheatTriggerAlarmClock()));
	}

	private OobHandler OobAction(Action<DefuserConnector> action) => this.OobAction((c, _, _) => action(c));
	private OobHandler OobAction(Action<DefuserConnector, XElement, Response> action) => (e, r) => {
		try {
			action.Invoke(this.connector, e, r);
		} catch (Exception ex) {
			r.User.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	};
	private OobHandler OobAction(Func<DefuserConnector, XElement, Response, Task> action) => async (e, r) => {
		try {
			await action.Invoke(this.connector, e, r);
		} catch (Exception ex) {
			r.User.Postback($"OOB DefuserSocketError {ex.Message}");
		}
	};
	
	public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) {
		var tokens = text.Split();
		switch (tokens[0].ToLower()) {
			case "getsidewidgetadjustment": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				return this.connector.GetSideWidgetAdjustment(screenshotBitmap).ToString();
			}
			case "identifymodule": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return this.connector.GetComponentReader(screenshotBitmap, quadrilateral)?.GetType().Name ?? "nil";
			}
			case "identifywidget": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return this.connector.GetWidgetReader(screenshotBitmap, quadrilateral)?.GetType().Name ?? "nil";
			}
			case "getlightstate": {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return this.connector.GetModuleLightState(screenshotBitmap, quadrilateral).ToString();
			}
			case "read": {
				return this.connector.CheatRead(new(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])), tokens.Skip(5).ToArray()) ?? "nil";
			}
			case "getmodulename": {
				return this.connector.CheatGetComponentReader(new(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4])))?.GetType().Name ?? "nil";
			}
			default: {
				var screenshotBitmap = cachedScreenshots[tokens[1]];
				var quadrilateral = new Quadrilateral(new(int.Parse(tokens[2]), int.Parse(tokens[3])), new(int.Parse(tokens[4]), int.Parse(tokens[5])), new(int.Parse(tokens[6]), int.Parse(tokens[7])), new(int.Parse(tokens[8]), int.Parse(tokens[9])));
				return this.connector.Read(tokens[0], screenshotBitmap, quadrilateral) ?? "nil";
			}
		}
	}
}
