namespace KtaneDefuserConnectorTests.Components;
public class KeypadTests {
	// TODO: Include the Circle symbol.
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Keypad1);
		var result = new Keypad().Process(image, LightsState.On, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList([Keypad.Symbol.Trident, Keypad.Symbol.Euro, Keypad.Symbol.Six, Keypad.Symbol.Ae], result.Symbols);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Keypad2);
		var result = new Keypad().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(1, 0), result.Selection);
		AssertList([Keypad.Symbol.LeftC, Keypad.Symbol.SquigglyN, Keypad.Symbol.UpsideDownY, Keypad.Symbol.SquidKnife], result.Symbols);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Keypad3);
		var result = new Keypad().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 1), result.Selection);
		AssertList([Keypad.Symbol.SquigglyN, Keypad.Symbol.AT, Keypad.Symbol.Balloon, Keypad.Symbol.HookN], result.Symbols);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Keypad4);
		var result = new Keypad().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		AssertList([Keypad.Symbol.Euro, Keypad.Symbol.HollowStar, Keypad.Symbol.HookN, Keypad.Symbol.LeftC], result.Symbols);
	}
}
