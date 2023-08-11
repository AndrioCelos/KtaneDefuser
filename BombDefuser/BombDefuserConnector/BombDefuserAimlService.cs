using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Aiml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
/// <summary>Integrates an AIML bot with Keep Talking and Nobody Explodes.</summary>
public class BombDefuserAimlService : ISraixService {
	private readonly DefuserConnector connector = new();
	private static readonly Dictionary<string, Image<Rgba32>> cachedScreenshots = new(StringComparer.InvariantCultureIgnoreCase);
	private static readonly Queue<string> cachedScreenshotIds = new();

	public BombDefuserAimlService() {
		this.connector.EnableCallbacks();
		AimlVoice.Program.OobHandlers["takescreenshot"] = this.OobAction(async (c, e) => {
			var token = e.Attributes["token"]?.Value ?? e.GetElementsByTagName("token").Cast<XmlNode>().FirstOrDefault()?.InnerText ?? "nil";
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
			AimlVoice.Program.sendInput($"OOB ScreenshotReady {token} {newId}");
		});
		AimlVoice.Program.OobHandlers["tasconnect"] = this.OobAction(async (c, e) => {
			await c.ConnectAsync(e.HasAttribute("simulation"));
			AimlVoice.Program.sendInput("OOB DefuserSocketConnected");
		});
		AimlVoice.Program.OobHandlers["sendinputs"] = this.OobAction((c, e) => c.SendInputs(e.InnerText));
		AimlVoice.Program.OobHandlers["solve"] = this.OobAction(c => c.CheatSolve());

		AimlVoice.Program.OobHandlers["strike"] = this.OobAction(c => c.CheatStrike());
		AimlVoice.Program.OobHandlers["triggeralarmclock"] = this.OobAction(c => c.CheatTriggerAlarmClock());
	}

	private Action<XmlElement> OobAction(Action<DefuserConnector> action) => this.OobAction((e, _) => action(e));
	private Action<XmlElement> OobAction(Action<DefuserConnector, XmlElement> action) => e => {
		try {
			action.Invoke(this.connector, e);
		} catch (Exception ex) {
			AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
		}
	};
	private Action<XmlElement> OobAction(Func<DefuserConnector, XmlElement, Task> action) => async e => {
		try {
			await action.Invoke(this.connector, e);
		} catch (Exception ex) {
			AimlVoice.Program.sendInput($"OOB DefuserSocketError {ex.Message}");
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
