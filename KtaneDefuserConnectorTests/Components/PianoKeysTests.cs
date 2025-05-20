namespace KtaneDefuserConnectorTests.Components;
public class PianoKeysTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.PianoKeys1);
		var result = new PianoKeys().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(1, 0), result.Selection);
		AssertList([PianoKeys.Symbol.Mordent, PianoKeys.Symbol.CutCommonTime, PianoKeys.Symbol.Natural], result.Symbols);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.PianoKeys2);
		var result = new PianoKeys().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		AssertList([PianoKeys.Symbol.Fermata, PianoKeys.Symbol.CutCommonTime, PianoKeys.Symbol.CommonTime], result.Symbols);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.PianoKeys3);
		var result = new PianoKeys().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(11, 0), result.Selection);
		AssertList([PianoKeys.Symbol.Natural, PianoKeys.Symbol.Fermata, PianoKeys.Symbol.Mordent], result.Symbols);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.PianoKeys4);
		var result = new PianoKeys().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList([PianoKeys.Symbol.CClef, PianoKeys.Symbol.Fermata, PianoKeys.Symbol.Mordent], result.Symbols);
	}
}
