namespace KtaneDefuserConnectorTests.Components;
public class NeedyMathTests {
	[Fact]
	public void Sample1() {
		using var ms = new MemoryStream(Resources.NeedyMath1);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(null, 18, "76+3"), result);
	}

	[Fact]
	public void Sample2() {
		using var ms = new MemoryStream(Resources.NeedyMath2);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(0, 0), 18, "85+36"), result);
	}

	[Fact]
	public void Sample3() {
		using var ms = new MemoryStream(Resources.NeedyMath3);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(2, 2), 7, "0-51"), result);
	}

	[Fact]
	public void Sample4() {
		using var ms = new MemoryStream(Resources.NeedyMath4);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(3, 2), 10, "44-69"), result);
	}

	[Fact]
	public void Sample5() {
		using var ms = new MemoryStream(Resources.NeedyMath5);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(3, 0), 17, "74+65"), result);
	}
}
