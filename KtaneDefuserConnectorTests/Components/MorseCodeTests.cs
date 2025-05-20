namespace KtaneDefuserConnectorTests.Components;
public class MorseCodeTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.MorseCode1);
		var result = new MorseCode().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(2, 0), false), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.MorseCode2);
		var result = new MorseCode().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(0, 0), false), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.MorseCode3);
		var result = new MorseCode().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(new(1, 1), true), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.MorseCode4);
		var result = new MorseCode().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, true), result);
	}
}
