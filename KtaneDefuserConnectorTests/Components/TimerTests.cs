using KtaneDefuserConnector.DataTypes;
using Timer = KtaneDefuserConnector.Components.Timer;

namespace KtaneDefuserConnectorTests.Components;
public class TimerTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Timer1);
		var result = new Timer().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(GameMode.Normal, 19 * 60 + 58, 0, 0), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Timer2);
		var result = new Timer().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(GameMode.Time, 14, 60, 0), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Timer3);
		var result = new Timer().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(GameMode.Steady, 7 * 60 + 21, 0, 1), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Timer4);
		var result = new Timer().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(GameMode.Zen, 0, 0, 2), result);
	}
}
