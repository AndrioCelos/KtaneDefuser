namespace KtaneDefuserConnectorTests.Components;
public class AnagramsTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Anagrams1);
		var result = new Anagrams().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(1, 1), result.Selection);
		AssertList(['P', 'O', 'O', 'L', 'E', 'D'], result.Letters);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Anagrams2);
		var result = new Anagrams().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(0, 1), result.Selection);
		AssertList(['S', 'T', 'R', 'E', 'A', 'M'], result.Letters);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Anagrams3);
		var result = new Anagrams().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(3, 0), result.Selection);
		AssertList(['C', 'E', 'L', 'L', 'A', 'R'], result.Letters);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Anagrams4);
		var result = new Anagrams().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList(['S', 'E', 'D', 'A', 'T', 'E'], result.Letters);
	}
}
