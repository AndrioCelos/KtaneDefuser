namespace KtaneDefuserConnectorTests.Components;
public class WiresTests {
	[Fact]
	public void Sample1() {
		var image = Image.Load<Rgba32>(Resources.Wires1);
		var result = new Wires().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(0, 5), result.Selection);
		AssertList([Wires.Colour.White, Wires.Colour.Black, Wires.Colour.Blue, Wires.Colour.White, Wires.Colour.White, Wires.Colour.Red], result.Colours);
	}

	[Fact]
	public void Sample2() {
		var image = Image.Load<Rgba32>(Resources.Wires2);
		var result = new Wires().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(0, 3), result.Selection);
		AssertList([Wires.Colour.Yellow, Wires.Colour.Black, Wires.Colour.White, Wires.Colour.White, Wires.Colour.Blue, Wires.Colour.Yellow], result.Colours);
	}

	[Fact]
	public void Sample3() {
		var image = Image.Load<Rgba32>(Resources.Wires3);
		var result = new Wires().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 1), result.Selection);
		AssertList([Wires.Colour.Red, Wires.Colour.Black, Wires.Colour.Red, Wires.Colour.Red], result.Colours);
	}

	[Fact]
	public void Sample4() {
		var image = Image.Load<Rgba32>(Resources.Wires4);
		var result = new Wires().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		AssertList([Wires.Colour.Black, Wires.Colour.Blue, Wires.Colour.Yellow, Wires.Colour.White, Wires.Colour.Black, Wires.Colour.Red], result.Colours);
	}
}
