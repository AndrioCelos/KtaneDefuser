namespace KtaneDefuserConnectorTests.Components;
public class WhosOnFirstTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.WhosOnFirst1);
		var result = new WhosOnFirst().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(1, 1), result.Selection);
		Assert.Equal(0, result.StagesCleared);
		Assert.Empty(result.Display);
		AssertList(["YOU’RE", "NEXT", "DONE", "UH HUH", "YOUR", "SURE"], result.Keys);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.WhosOnFirst2);
		var result = new WhosOnFirst().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(0, 2), result.Selection);
		Assert.Equal(1, result.StagesCleared);
		Assert.Equal("THEY’RE", result.Display);
		AssertList(["BLANK", "UHHH", "READY", "NOTHING", "OKAY", "WAIT"], result.Keys);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.WhosOnFirst3);
		var result = new WhosOnFirst().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		Assert.Equal(0, result.StagesCleared);
		Assert.Equal("NOTHING", result.Display);
		AssertList(["NEXT", "YOU’RE", "YOU ARE", "UR", "HOLD", "YOUR"], result.Keys);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.WhosOnFirst4);
		var result = new WhosOnFirst().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(0, result.StagesCleared);
		Assert.Equal("UR", result.Display);
		AssertList(["HOLD", "YOU", "YOUR", "LIKE", "YOU ARE", "NEXT"], result.Keys);
	}
}
