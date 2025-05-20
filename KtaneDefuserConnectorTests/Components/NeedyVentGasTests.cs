namespace KtaneDefuserConnectorTests.Components;
public class NeedyVentGasTests {
	[Fact]
	public void Sample1_LightsOnInactive() {
		var image = Image.Load<Rgba32>(Resources.NeedyVentGas1);
		var result = new NeedyVentGas().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(0, 0), null, null), result);
	}

	[Fact]
	public void Sample2_LightsOnActive() {
		var image = Image.Load<Rgba32>(Resources.NeedyVentGas2);
		var result = new NeedyVentGas().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 0), 35, "VENT GAS?"), result);
	}

	[Fact]
	public void Sample3_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.NeedyVentGas3);
		var result = new NeedyVentGas().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(1, 0), 19, "DETONATE?"), result);
	}

	[Fact]
	public void Sample4_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.NeedyVentGas4);
		var result = new NeedyVentGas().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(null, 0, "VENT GAS?"), result);
	}

	[Fact]
	public void Sample5_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.NeedyVentGas5);
		var result = new NeedyVentGas().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, null, "DETONATE?"), result);
	}
}
