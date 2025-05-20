namespace KtaneDefuserConnectorTests.Components;
public class LightsOutTests {
	[Fact]
	public void Sample1_LightsOnInactive() {
		var image = Image.Load<Rgba32>(Resources.LightsOut1);
		var result = new LightsOut().Process(image, LightsState.On, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Null(result.Time);
		AssertList(new bool[9], result.Lights);
	}

	[Fact]
	public void Sample2_LightsOnActive() {
		var image = Image.Load<Rgba32>(Resources.LightsOut2);
		var result = new LightsOut().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(2, 1), result.Selection);
		Assert.Equal(21, result.Time);
		AssertList([true, false, true, false, true, true, false, true, false], result.Lights);
	}

	[Fact]
	public void Sample3_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.LightsOut3);
		var result = new LightsOut().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(2, 2), result.Selection);
		Assert.Equal(20, result.Time);
		AssertList([false, false, true, false, false, true, true, true, true], result.Lights);
	}

	[Fact]
	public void Sample4_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.LightsOut4);
		var result = new LightsOut().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		Assert.Equal(11, result.Time);
		AssertList([false, false, true, true, true, false, true, true, false], result.Lights);
	}

	[Fact]
	public void Sample5_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.LightsOut5);
		var result = new LightsOut().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(8, result.Time);
		AssertList([false, false, true, false, false, false, false, false, false], result.Lights);
	}
}
