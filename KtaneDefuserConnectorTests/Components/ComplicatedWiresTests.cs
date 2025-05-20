namespace KtaneDefuserConnectorTests.Components;
public class ComplicatedWiresTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.ComplicatedWires1);
		var result = new ComplicatedWires().Process(image, LightsState.On, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList([
			ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.None,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star
		], result.Wires);
	}

	[Fact]
	public void Sample2_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.ComplicatedWires2);
		var result = new ComplicatedWires().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(4, 0), result.Selection);
		AssertList([
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue,
			ComplicatedWires.WireFlags.Red,
			ComplicatedWires.WireFlags.Blue,
			ComplicatedWires.WireFlags.Star | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star
		], result.Wires);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.ComplicatedWires3);
		var result = new ComplicatedWires().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(4, 0), result.Selection);
		AssertList([
			ComplicatedWires.WireFlags.Red,
			ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Star | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star | ComplicatedWires.WireFlags.Light
		], result.Wires);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.ComplicatedWires4);
		var result = new ComplicatedWires().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(2, 0), result.Selection);
		AssertList([
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Red,
			ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Blue,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star | ComplicatedWires.WireFlags.Light,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Light
		], result.Wires);
	}

	[Fact]
	public void Sample5_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.ComplicatedWires5);
		var result = new ComplicatedWires().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(5, 0), result.Selection);
		AssertList([
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Star,
			ComplicatedWires.WireFlags.Red | ComplicatedWires.WireFlags.Blue | ComplicatedWires.WireFlags.Star
		], result.Wires);
	}
}
