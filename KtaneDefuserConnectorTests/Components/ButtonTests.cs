namespace KtaneDefuserConnectorTests.Components;
public class ButtonTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Button1);
		var result = new Button().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(Button.Colour.White, "DETONATE", null), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Button2);
		var result = new Button().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(Button.Colour.Red, "HOLD", Button.Colour.Red), result);
	}

	[Fact]
	public void Sample3_LightsBuzzBlackLabel() {
		var image = Image.Load<Rgba32>(Resources.Button3);
		var result = new Button().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(Button.Colour.Yellow, "PRESS", null), result);
	}

	[Fact]
	public void Sample4_LightsBuzzWhiteLabel() {
		var image = Image.Load<Rgba32>(Resources.Button4);
		var result = new Button().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(Button.Colour.Red, "ABORT", null), result);
	}

	[Fact]
	public void Sample5_LightsOffBlackLabel() {
		var image = Image.Load<Rgba32>(Resources.Button5);
		var result = new Button().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(Button.Colour.Yellow, "ABORT", Button.Colour.White), result);
	}

	[Fact]
	public void Sample6_LightsOffWhiteLabel() {
		var image = Image.Load<Rgba32>(Resources.Button6);
		var result = new Button().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(Button.Colour.Blue, null, Button.Colour.Blue), result);
	}
}
