using KtaneDefuserConnector;
using KtaneDefuserConnectorApi;
using SixLabors.ImageSharp.Formats.Bmp;

namespace KtaneDefuserScripts;

[AimlInterface]
internal class Test {
	[AimlCategory("test input *")]
	public static async Task TestInputs(AimlAsyncContext context, string inputs) {
		using var interrupt = await Interrupt.EnterAsync(context);
		interrupt.SendInputs(inputs.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => {
			var tokens = s.Split(':');
			return (IInputAction) (tokens[0].ToLowerInvariant() switch {
				"rx" => new AxisAction(Axis.RightStickX, float.Parse(tokens[1])),
				"ry" => new AxisAction(Axis.RightStickY, float.Parse(tokens[1])),
				"a" => new ButtonAction(Button.A),
				"b" => new ButtonAction(Button.B),
				"up" => new ButtonAction(Button.Up),
				"down" => new ButtonAction(Button.Down),
				"left" => new ButtonAction(Button.Left),
				"right" => new ButtonAction(Button.Right),
				_ => throw new ArgumentException("Unknown input")
			});
		}));
	}

	[AimlCategory("test inputasync *")]
	public static async Task TestInputsAsync(AimlAsyncContext context, string inputs) {
		using var interrupt = await Interrupt.EnterAsync(context);
		await interrupt.SendInputsAsync(inputs.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => {
			var tokens = s.Split(':');
			return (IInputAction) (tokens[0].ToLowerInvariant() switch {
				"rx" => new AxisAction(Axis.RightStickX, float.Parse(tokens[1])),
				"ry" => new AxisAction(Axis.RightStickY, float.Parse(tokens[1])),
				"a" => new ButtonAction(Button.A),
				"b" => new ButtonAction(Button.B),
				"up" => new ButtonAction(Button.Up),
				"down" => new ButtonAction(Button.Down),
				"left" => new ButtonAction(Button.Left),
				"right" => new ButtonAction(Button.Right),
				_ => throw new ArgumentException("Unknown input")
			});
		}));
		interrupt.Context.Reply("Done.");
	}

	[AimlCategory("test screenshot")]
	public static void TestScreenshot(AimlAsyncContext context) {
		var image = KtaneDefuserConnector.DefuserConnector.Instance.TakeScreenshot();
		using var fileStream = File.OpenWrite(Path.Combine(Path.GetTempPath(), "test.bmp"));
		image.Save(fileStream, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
		context.Reply("Done.");
	}

	[AimlCategory("test getmoduletype * * *")]
	public static void TestGetModuleType(AimlAsyncContext context, int face, int x, int y) => context.Reply(KtaneDefuserConnector.DefuserConnector.Instance.CheatGetComponentReader(new(0, face, x, y))?.Name ?? "nil");

	[AimlCategory("test read * * * *")]
	public static void TestRead(AimlAsyncContext context, int face, int x, int y, string members) => context.Reply(KtaneDefuserConnector.DefuserConnector.Instance.CheatRead(new(0, face, x, y), members.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries)) ?? "nil");
}
