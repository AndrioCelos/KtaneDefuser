namespace KtaneDefuserConnectorTests.Components;
public class SwitchesTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Switches1);
		var result = new Switches().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		AssertList([false, true, true, false, false], result.CurrentState);
		AssertList([true, false, false, false, true], result.TargetState);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Switches2);
		var result = new Switches().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(4, 0), result.Selection);
		AssertList([true, true, true, true, true], result.CurrentState);
		AssertList([false, true, false, false, false], result.TargetState);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Switches3);
		var result = new Switches().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(3, 0), result.Selection);
		AssertList([true, false, false, false, true], result.CurrentState);
		AssertList([false, false, false, true, false], result.TargetState);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Switches4);
		var result = new Switches().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList([true, false, false, false, false], result.CurrentState);
		AssertList([true, false, true, false, true], result.TargetState);
	}
}
