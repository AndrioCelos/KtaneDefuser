namespace KtaneDefuserConnectorTests.Components;
public class NeedyKnobTests {
	// TODO: Test reading a non-focused module.
	[Fact]
	public void Sample1_LightsOnInactive() {
		var image = Image.Load<Rgba32>(Resources.NeedyKnob1);
		var result = new NeedyKnob().Process(image, LightsState.On, ref DummyImage);
		Assert.Null(result.Time);
		AssertList(new bool[12], result.Lights);
	}

	[Fact]
	public void Sample2_LightsOnActive() {
		var image = Image.Load<Rgba32>(Resources.NeedyKnob2);
		var result = new NeedyKnob().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(9, result.Time);
		AssertList([false, false, false, false, true, false, false, false, false, true, true, false], result.Lights);
	}

	[Fact]
	public void Sample3_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.NeedyKnob3);
		var result = new NeedyKnob().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(35, result.Time);
		AssertList([true, false, true, false, true, false, false, true, true, false, true, true], result.Lights);
	}

	[Fact]
	public void Sample4_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.NeedyKnob4);
		var result = new NeedyKnob().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(11, result.Time);
		AssertList([true, false, true, false, true, false, false, true, true, false, true, true], result.Lights);
	}

	[Fact]
	public void Sample5_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.NeedyKnob5);
		var result = new NeedyKnob().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(30, result.Time);
		AssertList([true, false, true, false, true, false, false, true, false, false, false, true], result.Lights);
	}
}
