namespace KtaneDefuserConnectorTests.Components;
public class EmojiMathTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.EmojiMath1);
		var result = new EmojiMath().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 1), "=))=-|::)"), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.EmojiMath2);
		var result = new EmojiMath().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(3, 0), "(:)=-:||:"), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.EmojiMath3);
		var result = new EmojiMath().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(new(0, 0), "=():-)==)"), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.EmojiMath4);
		var result = new EmojiMath().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, ")=+:(:|"), result);
	}
}
