namespace KtaneDefuserConnectorTests.Components;
public class PasswordTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Password1);
		var result = new Password().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(2, 2), result.Selection);
		AssertList(['P', 'Q', 'W', 'R', 'P'], result.Display);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Password2);
		var result = new Password().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(2, 1), result.Selection);
		AssertList(['D', 'A', 'C', 'O', 'V'], result.Display);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Password3);
		var result = new Password().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		AssertList(['W', 'S', 'J', 'F', 'N'], result.Display);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Password4);
		var result = new Password().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList(['H', 'P', 'F', 'B', 'T'], result.Display);
	}
}
