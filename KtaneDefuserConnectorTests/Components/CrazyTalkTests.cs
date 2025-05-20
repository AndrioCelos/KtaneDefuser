namespace KtaneDefuserConnectorTests.Components;
public class CrazyTalkTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.CrazyTalk1);
		var result = new CrazyTalk().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal("NOVEBMER OSCAR SPACE, LIMA INDIA TANGO ECHO ROMEO ALPHA LIMA LIMA YANKEE SPACE NOVEMBER OSCAR TANGO HOTEL INDIA NOVEMBER GOLF", result.Display.Replace(".", ""));
		Assert.False(result.SwitchIsDown);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.CrazyTalk2);
		var result = new CrazyTalk().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new("STOP.", true), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.CrazyTalk3);
		var result = new CrazyTalk().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new("132 FOR", false), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.CrazyTalk4);
		var result = new CrazyTalk().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new("←←→ ←→→", true), result);
	}
}
