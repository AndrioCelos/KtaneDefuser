namespace KtaneDefuserConnectorTests.Components;
public class MemoryTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Memory1);
		var result = new Memory().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(1, 0), result.Selection);
		Assert.Equal(1, result.Display);
		Assert.Equal(0, result.StagesCleared);
		AssertList([2, 1, 3, 4], result.Keys);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Memory2);
		var result = new Memory().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(3, 0), result.Selection);
		Assert.Equal(2, result.Display);
		Assert.Equal(1, result.StagesCleared);
		AssertList([3, 4, 1, 2], result.Keys);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Memory3);
		var result = new Memory().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(3, 0), result.Selection);
		Assert.Equal(3, result.Display);
		Assert.Equal(2, result.StagesCleared);
		AssertList([1, 4, 2, 3], result.Keys);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Memory4);
		var result = new Memory().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(4, result.Display);
		Assert.Equal(0, result.StagesCleared);
		AssertList([1, 3, 2, 4], result.Keys);
	}
}
