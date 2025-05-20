namespace KtaneDefuserConnectorTests.Components;
public class WordScrambleTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.WordScramble1);
		var result = new WordScramble().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(2, 1), result.Selection);
		AssertList(['O', 'B', 'A', 'K', 'M', 'O'], result.Letters);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.WordScramble2);
		var result = new WordScramble().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(3, 1), result.Selection);
		AssertList(['T', 'K', 'R', 'E', 'C', 'O'], result.Letters);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.WordScramble3);
		var result = new WordScramble().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		AssertList(['A', 'A', 'O', 'T', 'T', 'W'], result.Letters);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.WordScramble4);
		var result = new WordScramble().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList(['A', 'K', 'A', 'T', 'T', 'C'], result.Letters);
	}
}
