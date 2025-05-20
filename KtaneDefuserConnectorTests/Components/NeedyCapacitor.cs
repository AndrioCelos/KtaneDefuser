namespace KtaneDefuserConnectorTests.Components;
public class NeedyCapacitorTests {
	[Fact]
	public void Sample1() {
		using var ms = new MemoryStream(Resources.NeedyCapacitor1);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyCapacitor().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(null), result);
	}

	[Fact]
	public void Sample2() {
		using var ms = new MemoryStream(Resources.NeedyCapacitor2);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyCapacitor().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(23), result);
	}

	[Fact]
	public void Sample3() {
		using var ms = new MemoryStream(Resources.NeedyCapacitor3);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyCapacitor().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(null), result);
	}

	[Fact]
	public void Sample4() {
		using var ms = new MemoryStream(Resources.NeedyCapacitor4);
		var image = Image.Load<Rgba32>(ms);
		var result = new NeedyCapacitor().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null), result);
	}
}
