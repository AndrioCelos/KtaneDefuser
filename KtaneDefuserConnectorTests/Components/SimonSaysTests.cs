using KtaneDefuserConnector.DataTypes;

namespace KtaneDefuserConnectorTests.Components;
public class SimonSaysTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.SimonSays1);
		var result = new SimonSays().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(0, 1), null), result);
	}

	[Fact]
	public void Sample2_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.SimonSays2);
		var result = new SimonSays().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 2), SimonColour.Blue), result);
	}

	[Fact]
	public void Sample3_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.SimonSays3);
		var result = new SimonSays().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(0, 1), SimonColour.Red), result);
	}

	[Fact]
	public void Sample4_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.SimonSays4);
		var result = new SimonSays().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(new(1, 0), SimonColour.Yellow), result);
	}

	[Fact]
	public void Sample5_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.SimonSays5);
		var result = new SimonSays().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, SimonColour.Green), result);
	}
}
