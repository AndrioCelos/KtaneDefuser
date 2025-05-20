namespace KtaneDefuserConnectorTests.Components;
public class ColourFlashTests {
	[Fact]
	public void Sample1() {
		using var ms = new MemoryStream(Resources.ColourFlash1);
		var image = Image.Load<Rgba32>(ms);
		var result = new ColourFlash().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(0, 0), null, ColourFlash.Colour.None), result);
	}

	[Fact]
	public void Sample2() {
		using var ms = new MemoryStream(Resources.ColourFlash2);
		var image = Image.Load<Rgba32>(ms);
		var result = new ColourFlash().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 0), "RED", ColourFlash.Colour.Green), result);
	}

	[Fact]
	public void Sample3() {
		using var ms = new MemoryStream(Resources.ColourFlash3);
		var image = Image.Load<Rgba32>(ms);
		var result = new ColourFlash().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(null, "YELLOW", ColourFlash.Colour.Red), result);
	}
}
