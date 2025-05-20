namespace KtaneDefuserConnectorTests.Components;
public class LetterKeysTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.LetterKeys1);
		var result = new LetterKeys().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(1, 1), result.Selection);
		Assert.Equal(4, result.Display);
		AssertList(['B', 'C', 'A', 'D'], result.ButtonLabels);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.LetterKeys2);
		var result = new LetterKeys().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(1, 0), result.Selection);
		Assert.Equal(41, result.Display);
		AssertList(['A', 'B', 'D', 'C'], result.ButtonLabels);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.LetterKeys3);
		var result = new LetterKeys().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 1), result.Selection);
		Assert.Equal(57, result.Display);
		AssertList(['B', 'A', 'D', 'C'], result.ButtonLabels);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.LetterKeys4);
		var result = new LetterKeys().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(81, result.Display);
		AssertList(['A', 'C', 'B', 'D'], result.ButtonLabels);
	}
}
